# Architecture

`world-cup-typer` is a monorepo with a React PWA frontend, a layered .NET backend, PostgreSQL persistence, and GitHub Actions based delivery.

```text
frontend/  React + Vite + TypeScript + Tailwind + React Router + TanStack Query
backend/   .NET 8 solution split into Api, Application, Domain, Infrastructure, Tests
docs/      Product, architecture, API, data model, deployment, and operations notes
```

## Backend

The backend follows a layered structure:

- `Domain` contains entities and enums: users, teams, matches, predictions, results, leaderboard snapshots, push subscriptions, notification preferences, and notification deliveries.
- `Application` contains DTOs, service interfaces, use-case services, scoring, settlement, ranking, prediction rules, player/team management, and notification preference/subscription services.
- `Infrastructure` contains EF Core/PostgreSQL persistence, migrations, JWT generation, password hashing, development seed, football-data.org import, web push delivery, and hosted workers.
- `Api` contains controllers, auth/CORS configuration, Swagger in development, exception handling, password-change enforcement, and health endpoints.
- `Tests` covers the core business rules and integration-like service behavior with EF-backed test contexts.

## Runtime Services

The API registers these important infrastructure services:

- `DatabaseInitializer` applies migrations at startup when configured and can run development seed data.
- `FootballDataSyncWorker` periodically imports fixtures/results when `FootballData__Enabled=true`.
- `NotificationReminderWorker` periodically sends due web push reminders.
- `WebPushNotificationService` sends morning digests, missing-prediction reminders, ranking updates, and test notifications.
- `FootballDataScheduleImportService` upserts teams/matches, maps provider statuses/scores, and can auto-settle finished matches that have a 90-minute score.

## Frontend

The frontend is organized by product area:

- `app/` contains the app shell, protected route logic, query client, and formatting helpers.
- `api/` contains the typed HTTP client and API service wrappers.
- `components/` contains shared UI such as `Panel`, `QueryState`, `ResponsiveTable`, `MatchCard`, `StatCard`, alerts, form fields, and avatars.
- `features/` contains routed screens for public, auth, dashboard, matches, ranking, profile, admin, and PWA behavior.

Routes:

- `/` public landing for anonymous users, authenticated dashboard for users.
- `/login`
- `/change-password`
- `/matches`
- `/matches/:matchId`
- `/ranking`
- `/profile`
- `/admin`
- `/admin/players`
- `/admin/teams`
- `/admin/matches`

## Core Flows

### Prediction Flow

1. User opens matches or match details.
2. Frontend uses `/api/matches` or `/api/matches/{id}`.
3. Backend includes current user's prediction and `CanEditPrediction`.
4. User can create/update a prediction only before kickoff.
5. Backend enforces the same rule via `Match.CanAcceptPredictions(nowUtc)`.
6. Other users' predictions are visible only after kickoff.

### Settlement And Ranking

1. Admin saves the 90-minute result.
2. Admin or automated import settles the match.
3. `MatchSettlementService` calculates prediction results with `ScoringService`.
4. `LeaderboardBuilder` calculates ranking, tie-breakers, and positions.
5. `LeaderboardSnapshot` records the post-match state for progress views.
6. Ranking update notifications can be sent through web push.

### Web Push

1. User enables notifications in the profile page.
2. Frontend checks browser/PWA support, handles iOS install requirements, and registers the push service worker.
3. Backend stores subscription data with `DeviceId`, endpoint, VAPID keys, and user agent.
4. Notification preferences control morning digest, 2h reminder, 30m reminder, and ranking update notifications.
5. `NotificationDelivery` records attempted, sent, failed, and revoked deliveries for deduplication and diagnostics.

### Football Data Import

1. Import can run from the admin endpoint or scheduled worker.
2. `FootballDataClient` reads provider matches.
3. Mapper/service normalizes teams, flags, match phase, venue, status, 90-minute score, final score, and winner.
4. Matches are upserted by external id or match number.
5. Finished matches with a 90-minute score can be settled automatically when configured.

## Delivery Architecture

- Frontend deploys to GitHub Pages through `.github/workflows/frontend-pages.yml`.
- Backend image builds and publishes to GHCR through `.github/workflows/backend-image.yml`.
- Backend deployment to DigitalOcean App Platform is manual through `.github/workflows/backend-app-platform.yml`.
- EF migrations can run at startup or through `.github/workflows/backend-migrations.yml`.
- Pull requests run CI and Playwright smoke.

## Extensibility Points

- `IScheduleImportService` isolates external schedule/result providers.
- `IFootballDataClient` isolates football-data.org HTTP access.
- `INotificationService` isolates notification delivery.
- `IKnockoutResolverService` is present for future knockout bracket resolution.
- `IDateTimeProvider` keeps time-dependent business rules testable.
