# Agent guardrails for `world-cup-typer`

## Shared rules
- Validate the touched area before claiming success.
- Keep changes small, safe, and reviewable.
- Prefer tests over manual-only validation.
- Never store secrets or sensitive production data in the repository.
- Never merge with red checks.

## Agent responsibilities
- `frontend-polish`
  - owns layout polish, copy consistency, mobile responsiveness, and accessibility.
- `backend-guard`
  - owns API safety, domain rules, DTO consistency, auth/authorization changes, and backend tests.
- `qa-smoke`
  - owns staging-first smoke coverage, Playwright maintenance, and artifact-based failure diagnosis.
- `release-check`
  - owns pre-release readiness checks, environment sanity, health checks, and release blocker summary.

## Environment policy
- Staging is the default environment for reproduction and E2E.
- Production is for light smoke validation only.
- Any action that changes production data requires an explicit human decision.

## Human-only actions
- Managing secrets in GitHub UI.
- Editing rulesets, branch protection, DNS, and hosting settings in external dashboards.
- Accepting irreversible production data changes.
