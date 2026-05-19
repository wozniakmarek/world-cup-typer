# Copilot instructions for `world-cup-typer`

## Monorepo layout
- `frontend/` - React + Vite + TypeScript app, PWA-ready UI.
- `backend/` - .NET 8 Web API with auth, betting rules, ranking, and admin flows.
- `docs/` - product, architecture, deployment, and operating notes.

## Default operating model
- Work staging-first. Use staging or a non-production environment for most validation and reproduction work.
- Treat production as smoke-only:
  - page opens,
  - login works,
  - key screens load,
  - health endpoints respond.
- Do not treat production as a playground for repeated admin actions, resets, or test data creation.

## What Copilot should actively do in this repo
- Implement focused frontend and backend changes with minimal blast radius.
- Add or update tests when business rules change.
- Triage failing CI and Playwright smoke runs.
- Improve docs, checklists, and PR summaries.
- Review PRs for regressions in auth, kickoff locking, scoring, ranking, and admin flows.

## What Copilot must not do
- Do not push directly to `main`; use branches and pull requests.
- Do not merge pull requests without required/positive review approval.
- Do not hardcode secrets, tokens, passwords, JWT keys, or connection strings.
- Do not rotate secrets, change GitHub rulesets, DNS, or hosting settings unless a human explicitly handles the required UI action.
- Do not mutate production data unless the task explicitly requires a safe, minimal production fix.
- Do not weaken deployment workflows or bypass failing checks.

## Required validation
- Backend:
  - `cd backend && dotnet restore`
  - `cd backend && dotnet build WorldCupTyper.sln --configuration Release --no-restore`
  - `cd backend && dotnet test WorldCupTyper.sln --configuration Release --no-build`
- Frontend:
  - `cd frontend && npm ci`
  - `cd frontend && npm run build`
  - `cd frontend && npm run test:e2e:smoke`

## Business rules that must never regress
- Scoring is `3/1/0`:
  - 3 points for exact 90-minute score,
  - 1 point for correct 1X2 outcome,
  - 0 otherwise.
- A prediction can be created or edited only before kickoff.
- Backend must enforce kickoff locking; UI-only protection is not enough.
- Other players' predictions become visible only after kickoff.
- Ranking order is:
  - total points,
  - exact hits,
  - correct outcomes,
  - predictions count,
  - player display name.

## Pull request expectations
- Keep changes scoped and easy to review.
- Do not merge without required review approval.
- Mention verification commands in the PR body or summary.
- If smoke tests are skipped because secrets are missing, state that explicitly.
- Prefer additive fixes over wide refactors unless the task is specifically a refactor.
