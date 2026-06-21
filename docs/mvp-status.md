# Project Status

This document reflects the current codebase state. The original MVP scope has been completed and the project has moved into production hardening and polish.

## Current Product State

The app is an end-to-end private World Cup prediction league:

- production frontend is deployed at `https://typer.marekwozniak.me`,
- backend is containerized and deployable to DigitalOcean App Platform,
- public users can see the landing page and anonymous top-ranking endpoint,
- invited users can log in, predict matches, view ranking/progress, manage profile/avatar, and configure notifications,
- admins can manage players, teams, matches, results, settlement, ranking recalculation, and football-data sync.

## Implemented Backend

- JWT authentication for `Admin` and `Player`.
- Role-based authorization for admin endpoints.
- Required password-change flow after temporary password reset.
- REST endpoints for auth, players, teams, matches, predictions, ranking, notifications, and admin operations.
- Backend-enforced prediction lock after kickoff.
- Prediction visibility rule: own prediction before kickoff, other players after kickoff.
- `3/1/0` scoring.
- Match settlement and ranking recalculation.
- `LeaderboardSnapshot` history for ranking progress.
- Profile avatar storage, including resized image data URLs from the frontend.
- EF Core PostgreSQL migrations.
- Startup database initialization/migration path.
- Development seed data for local use.
- Health endpoints `/health/live` and `/health`.
- football-data.org schedule/result import and optional automatic settlement.
- Hosted football-data sync worker.
- Web Push subscription, preferences, delivery records, reminders, ranking update notifications, and test notification endpoint.

## Implemented Frontend

- Public landing page and login page.
- Authenticated dashboard with open prediction counts, upcoming matches, top ranking, and scoring rules.
- Match list with filters and responsive cards.
- Match details with prediction form, kickoff lock messaging, result display, and post-kickoff predictions.
- Ranking table with avatars, tie-breaker data, current-user marker, and ranking progress chart.
- Profile page with stats, prediction history, ranking progress, password change, avatar URL/gallery upload, and notification settings.
- Admin dashboard with summary metrics.
- Admin player management with create/edit/deactivate/reset password flows.
- Admin team management.
- Admin match management with create/edit/result/settle/recalculate flows.
- PWA manifest, install prompt, service worker, icons, and push service worker.
- Web Push browser flow with iOS standalone guidance, VAPID key normalization, device id, enable/disable/test actions.
- Shared loading/error/empty/success UI states.
- Mobile navigation, responsive admin tables/cards, and iOS-safe form control sizing.

## Implemented Operations

- `docker-compose.yml` for local PostgreSQL.
- Local helper scripts `typer.ps1` and `typer.cmd`.
- Dockerfile for the API.
- GitHub Actions CI for backend build/test, frontend build, and API container build.
- GitHub Pages frontend deployment.
- GHCR backend image publication.
- Manual DigitalOcean App Platform backend deployment from a pinned image digest.
- Manual EF Core migration workflow.
- Playwright smoke workflow with production, staging, and local preview modes.
- Production backup runbook.

## Test Coverage

Current test inventory from the repository:

- 87 backend test cases across auth, password changes, players, matches, predictions, scoring, ranking, settlement, football-data import/mapping, web push options, notification services, notification workers, authorization, and migration cleanup behavior.
- 26 frontend/Playwright/helper tests across public smoke, login smoke, staging player/admin smoke, ranking progress, profile notifications, avatar upload, mobile navigation/layout, and football-data match presentation.

## Production Configuration

Production deployment is represented by:

- `.github/workflows/frontend-pages.yml`
- `.github/workflows/backend-image.yml`
- `.github/workflows/backend-app-platform.yml`
- `.do/app.yaml`

The DigitalOcean app spec configures:

- `api-typer.marekwozniak.me`,
- GHCR image digest deployment,
- PostgreSQL connection string through secrets,
- JWT through secrets,
- production CORS for `https://typer.marekwozniak.me`,
- football-data.org enabled with token from GitHub secrets,
- Web Push VAPID values from secrets/variables.

## Not In Scope Yet

These are optional future improvements, not missing MVP pieces:

- email password reset/invitations,
- OAuth or Google login,
- non-stub smart knockout resolver,
- deeper social features such as badges, post-match summaries, and head-to-head comparisons,
- richer production observability beyond the current workflow and health-check setup.
