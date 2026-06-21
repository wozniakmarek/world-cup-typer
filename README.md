# World Cup Typer

Production-ready web application for running a private World Cup prediction league with friends.

The app covers the full tournament workflow: player accounts, match predictions, kickoff locks, result settlement, public ranking, admin operations, PWA support, web push reminders, and CI/CD deployment. It is built as a portfolio project, but it is also a real product deployed for a private group.

## Live

- Frontend: [typer.marekwozniak.me](https://typer.marekwozniak.me)
- Public access: landing page and ranking
- Private access: predictions, profile, notifications, and admin panels require an invited account

## Screenshots

The screenshots below were captured from the production frontend. Ranking data is intentionally hidden in the public screenshot to avoid publishing player data in the repository.

| Public landing | Login |
| --- | --- |
| ![Public landing screen](docs/assets/screenshots/public-home.png) | ![Login screen](docs/assets/screenshots/login.png) |

## What It Does

World Cup Typer is a private PWA for predicting FIFA World Cup match results. Players submit scores before kickoff, the backend enforces prediction locks, and the system settles matches into a transparent `3/1/0` scoring model:

- `3 pts` for an exact score after 90 minutes
- `1 pt` for the correct outcome
- `0 pts` for a missed prediction

The app exposes a public competition surface while keeping the actual prediction workflow private.

## Product Highlights

- Player login with JWT authentication and role-based access.
- Admin panel for players, teams, matches, results, and settlement.
- Match list, match details, prediction form, kickoff lock, and post-kickoff prediction visibility.
- Ranking with deterministic tie-breakers and a ranking progress chart backed by leaderboard snapshots.
- Player profile with prediction history, avatar support, stats, and notification preferences.
- PWA installation flow, service worker setup, and browser push notification support.
- Web push reminders for missing predictions and ranking updates, with idempotent delivery tracking.
- Optional schedule/result import through a `football-data.org` adapter.
- Mobile-first dark UI with responsive tables, cards, loading states, empty states, and error handling.

## Architecture

The repository is a monorepo with a React frontend and a layered .NET backend.

```text
frontend/  React, Vite, TypeScript, Tailwind CSS, React Router, TanStack Query, PWA
backend/   .NET 8, ASP.NET Core Web API, EF Core, PostgreSQL, JWT, Web Push
docs/      Architecture, API contract, data model, deployment notes, product notes
```

Backend layers:

- `Domain` - entities, enums, and business concepts.
- `Application` - DTOs, service interfaces, and use-case services.
- `Infrastructure` - EF Core, PostgreSQL persistence, JWT, password hashing, football data import, notifications.
- `Api` - REST controllers, auth, middleware, CORS, health checks, and host configuration.
- `Tests` - unit and integration-style coverage for core domain rules.

Key design choices:

- Prediction rules are enforced in the backend, not only in the UI.
- Settlement is idempotent and produces leaderboard snapshots for historical ranking views.
- Push delivery uses durable records to deduplicate reminders and diagnose failed subscriptions.
- External integrations are behind interfaces so the core game logic stays testable.

## Tech Stack

| Area | Stack |
| --- | --- |
| Frontend | React, TypeScript, Vite, Tailwind CSS, React Router, TanStack Query, Recharts |
| Backend | .NET 8, ASP.NET Core Web API, EF Core, PostgreSQL |
| Auth | JWT, role-based authorization |
| PWA | Vite PWA, service worker, install prompt, Web Push |
| Integrations | football-data.org adapter, Web Push VAPID |
| Delivery | GitHub Actions, GitHub Pages, GHCR, DigitalOcean App Platform |
| QA | xUnit, Playwright smoke tests, Docker image build, health checks |

## Quality And Operations

The project includes automated checks and production-oriented workflows:

- CI builds and tests the backend.
- CI builds the frontend.
- Docker image build validates the API container.
- GitHub Pages workflow deploys the frontend.
- DigitalOcean App Platform workflow deploys the backend from a pinned image digest.
- Playwright smoke tests support production, staging, and local preview modes.
- Health endpoints expose liveness and readiness checks:

```text
GET /health
GET /health/live
```

## Local Development

### Requirements

- .NET 8 SDK
- Node.js 20+
- Docker

### 1. Start PostgreSQL

```bash
docker compose up -d
```

### 2. Run Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet test
dotnet tool restore
dotnet tool run dotnet-ef database update --project WorldCupTyper.Infrastructure --startup-project WorldCupTyper.Api
dotnet run --project WorldCupTyper.Api
```

The API starts at:

```text
http://localhost:5000
```

### 3. Run Frontend

```bash
cd frontend
npm install
copy ..\.env.example .env
npm run dev
```

The frontend expects:

```text
VITE_API_BASE_URL=http://localhost:5000
```

Local CORS allows both:

```text
http://localhost:5173
http://127.0.0.1:5173
```

## Development Seed

Development-only accounts are seeded for local testing:

```text
Admin:  admin@marekwozniak.me / ChangeMe123!
Players: marek@typer.local, kuba@typer.local, bartek@typer.local, pawel@typer.local, asia@typer.local
Password: ChangeMe123!
```

These credentials are not production credentials. Production configuration is provided through environment variables and secrets.

## Useful Commands

Backend:

```bash
dotnet test backend/WorldCupTyper.sln --configuration Release
docker build -f backend/WorldCupTyper.Api/Dockerfile -t world-cup-typer-api .
```

Frontend:

```bash
cd frontend
npm run build
npm run build:pages
npm run lint
npm run test:e2e:smoke
```

## Documentation

- [Architecture](docs/architecture.md)
- [API contract](docs/api-contract.md)
- [Data model](docs/database-model.md)
- [Local development](docs/local-development.md)
- [Deployment preparation](docs/deployment-prep.md)
- [Production backups](docs/production-backups.md)
- [Web push notifications](docs/web-push-notifications.md)
- [Football API research](docs/football-api-research.md)
- [MVP status](docs/mvp-status.md)

## Roadmap

The core product is already usable end-to-end. The next interesting improvements are:

- richer post-match summaries,
- stronger social comparison between players,
- deeper tournament analytics,
- more polished mobile review before wider public sharing,
- continued hardening of production observability and backup routines.
