# Football Data Automation Design

Date: 2026-05-19
Issue: #35

## Goal

Implement a fully automated football-data.org integration that imports World Cup schedule data, keeps match statuses and scores current, stores the 90-minute result used for scoring, and settles finished matches without manual admin action.

## Context

The app already has the key domain fields for this feature:

- `Match.ExternalId` can store provider match ids.
- `Match.HomeScore90` and `Match.AwayScore90` drive prediction scoring.
- `Match.HomeScoreFinal`, `Match.AwayScoreFinal`, and `WinnerTeamId` can display knockout results without changing 90-minute scoring.
- `MatchSettlementService` can settle a match once 90-minute scores are present.
- `LeaderboardSnapshot` records ranking state after settlement.
- `IScheduleImportService` currently exists as a stub in infrastructure.

The research in `docs/football-api-research.md` recommends football-data.org because its v4 API exposes `score.regularTime`, which maps directly to the app's 90-minute scoring model.

## Scope

This feature includes:

- football-data.org configuration with no secrets committed to the repository.
- an HTTP client for football-data.org v4 competition matches.
- provider DTOs and mapper code isolated in infrastructure.
- idempotent schedule and score import.
- safe mapping of provider statuses to `MatchStatus`.
- storage of provider match ids in `Match.ExternalId` using the `football-data:{id}` format.
- storage of provider team ids so future imports do not depend only on team names.
- automatic settlement after a finished match has a valid 90-minute result.
- a background worker that runs only when explicitly enabled and configured.
- an admin endpoint to trigger sync manually for staging, smoke checks, and recovery.
- documentation for required environment variables and operational behavior.

## Non-Goals

This feature does not add:

- a frontend admin screen for import controls.
- live in-match UI details such as clock, minute, or scorer events.
- API-Football fallback implementation.
- production secret management automation. Humans still manage secrets in GitHub, hosting dashboards, or environment configuration.

## Architecture

The integration stays in `Infrastructure`, behind application-facing interfaces. The application layer keeps the existing business rules for scoring and settlement.

New units:

- `FootballDataOptions`: typed config for API base URL, token, competition code, worker enablement, sync interval, and lookback/lookahead.
- `IFootballDataClient`: fetches competition matches from football-data.org.
- `FootballDataClient`: `HttpClient` implementation that sends `X-Auth-Token` and deserializes JSON.
- `FootballDataMatchMapper`: converts provider status, teams, kickoff, venue, and scores into internal sync models.
- `FootballDataScheduleImportService`: replaces the stub and performs idempotent upserts plus settlement triggering.
- `FootballDataSyncWorker`: background loop that periodically calls `IScheduleImportService` when enabled.

Existing units reused:

- `IAppDbContext` for persistence.
- `IMatchSettlementService` for point calculation and ranking snapshot creation.
- `IDateTimeProvider` for timestamps.
- `Match.CanAcceptPredictions(nowUtc)` remains the prediction lock rule; imported status is not the locking source of truth.

## Data Model

`Match.ExternalId` stores provider match ids as `football-data:{matchId}`.

`Team` gains a nullable `ExternalId` column with the same provider prefix pattern, for example `football-data:759`. This keeps the first implementation simple and reliable for one provider. If a second provider is added later, we can migrate to an `ExternalTeamMapping` table without changing the import flow.

Indexes:

- unique index on `Match.ExternalId` when not null.
- unique index on `Team.ExternalId` when not null.

## Configuration

Configuration section:

```json
{
  "FootballData": {
    "Enabled": false,
    "BaseUrl": "https://api.football-data.org/v4/",
    "ApiToken": "",
    "CompetitionCode": "WC",
    "SyncIntervalMinutes": 30,
    "LookbackDays": 2,
    "LookaheadDays": 370,
    "SettleAutomatically": true
  }
}
```

Environment variable examples:

- `FootballData__Enabled=true`
- `FootballData__ApiToken=<secret>`
- `FootballData__CompetitionCode=WC`
- `FootballData__SyncIntervalMinutes=30`
- `FootballData__SettleAutomatically=true`

The worker must not run unless `Enabled=true` and `ApiToken` is not blank. Manual admin sync should also reject missing token with a controlled application error.

## Sync Flow

Manual and background sync use the same application path:

1. Fetch matches from `/competitions/{CompetitionCode}/matches`.
2. Map each provider match to an internal sync record.
3. Upsert teams by `Team.ExternalId`. If a team has no external id yet, match by ISO-like country code first, then short name, then exact name.
4. Upsert matches by `Match.ExternalId`.
5. Update safe schedule fields: match number, phase, group, teams, kickoff, venue, and status.
6. If the provider has a safe 90-minute result, update `HomeScore90` and `AwayScore90`.
7. If final score exists, update `HomeScoreFinal`, `AwayScoreFinal`, and `WinnerTeamId`.
8. For finished, unsettled matches with 90-minute scores, call `MatchSettlementService.SettleMatchAsync`.
9. Return a sync summary with imported, updated, skipped, settled, and failed counts.

The sync must be idempotent. Running it twice against the same feed should not create duplicate teams, duplicate matches, duplicate results, or duplicate ranking snapshots.

## Status Mapping

Provider statuses map as follows:

- `SCHEDULED`, `TIMED` -> `Scheduled`
- `LIVE`, `IN_PLAY`, `PAUSED` -> `InProgress`
- `FINISHED` -> `Finished`, unless the match is already `Settled`
- `POSTPONED`, `SUSPENDED`, `CANCELLED` -> `Cancelled`

Unknown statuses are skipped and counted as skipped records. They should be logged, not guessed.

## Score Mapping

For settlement scoring:

- Use `score.regularTime.home` and `score.regularTime.away` when present.
- If `duration == REGULAR` and `score.regularTime` is missing, use `score.fullTime` as the 90-minute result.
- If `duration` indicates extra time or penalties and `score.regularTime` is missing, do not auto-settle.

For display/final winner:

- Use `score.fullTime` as final score when present.
- If penalties decide a tied knockout match and provider data exposes penalty scores, final display may still remain tied in `HomeScoreFinal` / `AwayScoreFinal`; `WinnerTeamId` should only be set when the winner can be resolved safely.
- If winner cannot be resolved safely, leave `WinnerTeamId` null and still settle by 90-minute result.

## Error Handling And Safety

- Missing API token disables the worker and makes manual sync return a controlled error.
- HTTP failures are logged and do not crash the process.
- One bad provider record should not fail the entire sync batch.
- Settlement errors are counted and logged per match.
- No API token or provider response body containing secrets is logged.
- Production data changes still require a human decision through environment enablement, matching the repository guardrails.

## API

Add an admin-only endpoint:

- `POST /api/admin/matches/sync-football-data`

Response body:

```json
{
  "importedMatches": 10,
  "updatedMatches": 12,
  "skippedMatches": 1,
  "settledMatches": 3,
  "failedMatches": 0
}
```

This endpoint is for staging smoke checks and manual recovery. The background worker is the normal automation path.

## Testing Strategy

Tests should cover:

- status mapping.
- 90-minute score mapping for regular-time matches.
- 90-minute score mapping for extra-time matches with `regularTime`.
- skip behavior when extra-time match lacks `regularTime`.
- team upsert by external id.
- match upsert by external id.
- idempotent second sync.
- automatic settlement for finished matches with 90-minute scores.
- no settlement for finished matches without safe 90-minute scores.
- worker disabled behavior when config is off or token is blank.

The main verification command is:

```powershell
dotnet test backend\WorldCupTyper.sln
```

## Rollout

1. Merge the implementation behind disabled default config.
2. Configure token and `FootballData__Enabled=true` in staging.
3. Trigger manual admin sync in staging and inspect imported matches.
4. Let the worker run one interval in staging.
5. Verify no duplicate matches or teams after repeated sync.
6. Enable production only after human approval and a staging smoke pass.
