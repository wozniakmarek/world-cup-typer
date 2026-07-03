# Deployment Preparation

This repository is already wired for production-style delivery. The frontend is deployed as a static GitHub Pages app, and the backend is deployed as a containerized .NET API to DigitalOcean App Platform.

## Frontend

- Production URL: `https://typer.marekwozniak.me`
- Workflow: `.github/workflows/frontend-pages.yml`
- Build command: `npm run build:pages`
- Build output: `frontend/dist`
- API env:
  - `VITE_API_BASE_URL=https://api-typer.marekwozniak.me`
  - `VITE_API_FALLBACK_BASE_URL=https://world-cup-typer-api-oznlp.ondigitalocean.app`

The Pages build creates `dist/404.html` so React Router deep links keep working after refresh.

## Backend

- Production app spec: `.do/app.yaml`
- Production domain: `api-typer.marekwozniak.me`
- Runtime: Docker image from GHCR
- Image name: `ghcr.io/wozniakmarek/world-cup-typer-api`
- Container port: `8080`
- Health endpoints:
  - `/health/live`
  - `/health`

The manual backend deployment workflow is `.github/workflows/backend-app-platform.yml`. It builds the API image, pushes it to GHCR, and deploys the exact image digest to DigitalOcean App Platform.

## Required Production Values

The backend app spec uses these important environment values:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer=WorldCupTyper`
- `Jwt__Audience=WorldCupTyper.Client`
- `Jwt__ExpiryMinutes=43200`
- `Cors__AllowedOrigins__0=https://typer.marekwozniak.me`
- `Seed__Enabled=false`
- `FootballData__Enabled=true`
- `FootballData__ApiToken`
- `FootballData__CompetitionCode=WC`
- `FootballData__SyncIntervalMinutes=30`
- `FootballData__SettleAutomatically=true`
- `WebPush__Subject`
- `WebPush__PublicKey`
- `WebPush__PrivateKey`

Secrets are supplied through GitHub Actions and DigitalOcean App Platform. They must not be committed to the repository.

## GitHub Actions

### CI

`.github/workflows/ci.yml` runs:

- backend restore/build/test,
- frontend `npm ci` and build,
- Docker image build for the API.

### Frontend Pages

`.github/workflows/frontend-pages.yml` deploys the frontend to GitHub Pages on `main` changes affecting `frontend/**` or the workflow.

### Backend Image

`.github/workflows/backend-image.yml` publishes the backend image to GHCR on `main` backend changes or manual dispatch.

Tags:

- `latest` for the default branch,
- `sha-<commit>`,
- branch ref tag.

For public repositories it can also generate a build provenance attestation.

### Backend App Platform

`.github/workflows/backend-app-platform.yml` is manual (`workflow_dispatch`) to avoid accidental production backend deploys. It requires:

- `DIGITALOCEAN_ACCESS_TOKEN`
- `GHCR_CREDENTIALS`
- `DEFAULT_CONNECTION`
- `JWT_KEY`
- `FOOTBALL_DATA_API_TOKEN`
- `WEB_PUSH_PUBLIC_KEY`
- `WEB_PUSH_PRIVATE_KEY`
- `WEB_PUSH_SUBJECT` as a variable or default URL fallback

### Backend Migrations

`.github/workflows/backend-migrations.yml` is manual and runs:

```bash
dotnet tool run dotnet-ef database update --project backend/WorldCupTyper.Infrastructure
```

using `ConnectionStrings__DefaultConnection` from the `DEFAULT_CONNECTION` secret.

## Database Migrations

There are two supported models:

1. Startup migrations through `DatabaseStartup__ApplyMigrationsOnStartup=true`.
2. Manual migration workflow before backend deploy.

The codebase supports both. Production changes that may affect real data should be reviewed before running migrations.

## football-data.org

The current production app spec enables football-data.org import:

```yaml
FootballData__Enabled=true
FootballData__CompetitionCode=WC
FootballData__SyncIntervalMinutes=30
FootballData__SettleAutomatically=true
```

The token comes from `FOOTBALL_DATA_API_TOKEN`. The import can also be triggered manually by an admin with:

```text
POST /api/admin/matches/sync-football-data
```

The importer upserts teams/matches, updates scores/statuses, and can auto-settle finished matches when a 90-minute score is available.

## Web Push

Production web push uses VAPID configuration:

- `WEB_PUSH_PUBLIC_KEY`
- `WEB_PUSH_PRIVATE_KEY`
- `WEB_PUSH_SUBJECT`

The app supports:

- browser/device subscription registration,
- per-account notification preferences,
- morning digest,
- 2h and 30m missing-prediction reminders,
- ranking update notification,
- test notification endpoint,
- delivery deduplication and expired-subscription revocation.

## Local Image Run

Example local production-style run:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="<postgres-connection-string>" \
  -e Jwt__Key="<jwt-secret>" \
  -e Jwt__Issuer="WorldCupTyper" \
  -e Jwt__Audience="WorldCupTyper.Client" \
  -e Seed__Enabled=false \
  ghcr.io/wozniakmarek/world-cup-typer-api:latest
```

## Release Guardrails

- Keep production data changes explicit and reviewed.
- Do not commit secrets, connection strings, VAPID private keys, API tokens, DB dumps, or production exports.
- Do not merge with failing required checks.
- Do not merge without required review approval.
