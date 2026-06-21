# Data Model

The backend uses EF Core with PostgreSQL. Domain entities live in `backend/WorldCupTyper.Domain/Entities`; EF configuration and migrations live in `backend/WorldCupTyper.Infrastructure/Persistence`.

## Main Entities

### ApplicationUser

Represents both admins and players.

- `Id`
- `Email`
- `DisplayName`
- `PasswordHash`
- `AvatarUrl`
- `Role`
- `IsActive`
- `RequiresPasswordChange`
- `CreatedAtUtc`
- `LastLoginAtUtc`

Navigation:

- `Predictions`
- `LeaderboardSnapshots`
- `NotificationPreference`
- `PushSubscriptions`
- `NotificationDeliveries`

### Team

Represents a national team in the tournament.

- `Id`
- `ExternalId`
- `Name`
- `ShortName`
- `CountryCode`
- `FlagEmoji`
- `GroupName`

Navigation:

- `HomeMatches`
- `AwayMatches`

### Match

Stores fixture, score, settlement, and knockout metadata.

- `Id`
- `ExternalId`
- `MatchNumber`
- `Phase`
- `GroupName`
- `HomeTeamId`
- `AwayTeamId`
- `HomeSlotRule`
- `AwaySlotRule`
- `KickoffTimeUtc`
- `Venue`
- `Status`
- `HomeScore90`
- `AwayScore90`
- `HomeScoreFinal`
- `AwayScoreFinal`
- `WinnerTeamId`
- `IsSettled`
- `SettledAtUtc`
- `CreatedAtUtc`
- `UpdatedAtUtc`

Business rule:

- predictions are accepted only while `KickoffTimeUtc > nowUtc`.

### Prediction

One user's prediction for one match.

- `Id`
- `UserId`
- `MatchId`
- `PredictedHomeScore`
- `PredictedAwayScore`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `LockedAtUtc`

### PredictionResult

Settlement result for one prediction.

- `Id`
- `PredictionId`
- `Points`
- `IsExactScore`
- `IsCorrectOutcome`
- `CalculatedAtUtc`

### LeaderboardSnapshot

Historical ranking state after a settled match. This powers progress views.

- `Id`
- `MatchId`
- `UserId`
- `TotalPoints`
- `ExactScoreHits`
- `CorrectOutcomeHits`
- `PredictionsCount`
- `Position`
- `CreatedAtUtc`

### PushSubscription

One browser/device subscription for web push.

- `Id`
- `UserId`
- `Endpoint`
- `P256dh`
- `Auth`
- `UserAgent`
- `DeviceId`
- `CreatedAtUtc`
- `LastSeenAtUtc`
- `RevokedAtUtc`
- `FailureCount`
- `LastFailureAtUtc`

### NotificationPreference

Account-level notification settings.

- `UserId`
- `MorningDigestEnabled`
- `MissingPrediction2hEnabled`
- `MissingPrediction30mEnabled`
- `RankingUpdatedEnabled`
- `QuietHoursStartLocal`
- `QuietHoursEndLocal`
- `UpdatedAtUtc`

### NotificationDelivery

Durable delivery and deduplication record for push notifications.

- `Id`
- `UserId`
- `PushSubscriptionId`
- `MatchId`
- `SubjectKey`
- `Type`
- `ScheduledForUtc`
- `SentAtUtc`
- `Status`
- `ErrorCode`
- `CreatedAtUtc`

## Enums

- `UserRole`: `Admin`, `Player`
- `MatchPhase`: `GroupStage`, `RoundOf32`, `RoundOf16`, `QuarterFinal`, `SemiFinal`, `ThirdPlace`, `Final`
- `MatchStatus`: `Scheduled`, `InProgress`, `Finished`, `Settled`, `Cancelled`
- `NotificationType`: `MorningDigest`, `MissingPrediction2h`, `MissingPrediction30m`, `RankingUpdated`, `Test`
- `NotificationDeliveryStatus`: `Pending`, `Sent`, `Skipped`, `Failed`

## Constraints And Indexes

The model enforces:

- unique user email,
- unique user display name,
- unique team name,
- unique team short name,
- unique match number,
- unique prediction per `UserId + MatchId`,
- unique leaderboard snapshot per `MatchId + UserId`,
- unique push subscription endpoint,
- unique notification delivery per `UserId + PushSubscriptionId + Type + SubjectKey + ScheduledForUtc`.

## Migration History

The current migration set covers:

- initial users, teams, matches, predictions, results, and leaderboard snapshots,
- profile avatar URLs,
- football-data external identifiers,
- cleanup of legacy/demo imported schedule data,
- required password-change flag,
- inactive-player cleanup,
- web push notification entities and preferences,
- push subscription device ids,
- expanded avatar URL storage for image data URLs.
