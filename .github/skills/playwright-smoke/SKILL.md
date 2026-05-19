# Playwright smoke (`world-cup-typer`)

## Goal
Fast verification that the application still works after changes:
- app opens,
- login page renders,
- optional admin/player login works.

## Local run
```bash
cd frontend
npm ci
npx playwright install --with-deps chromium
E2E_BASE_URL=http://127.0.0.1:4173 npm run test:e2e:smoke
```

## CI workflow
- Workflow file: `.github/workflows/playwright-smoke.yml`
- Triggers:
  - `pull_request`
  - `push` to `main`
  - `workflow_dispatch`

## Secrets
- `E2E_BASE_URL`
- `E2E_ADMIN_EMAIL`
- `E2E_ADMIN_PASSWORD`
- `E2E_PLAYER_EMAIL`
- `E2E_PLAYER_PASSWORD`

## Environment policy
- Preferred target is staging.
- Production is smoke-only:
  - page opens,
  - login works,
  - core screens load.
- Do not run repeated or destructive admin actions on production.

## Secret format
- Preferred: raw value only, for example `https://staging.example.com`
- Avoid storing secrets as `KEY=value`
- The workflow and tests tolerate `KEY=value`, but that is a fallback, not the target format

## Artifacts and logs
- GitHub Actions -> `Playwright Smoke`
- On failure download artifact `playwright-artifacts`:
  - `trace.zip`
  - screenshots
  - video

## Failure hints
- Login page load failure usually means:
  - bad `E2E_BASE_URL`,
  - frontend is down,
  - DNS or routing issue.
- Role login failure usually means:
  - bad credentials,
  - auth API issue,
  - backend availability problem.
