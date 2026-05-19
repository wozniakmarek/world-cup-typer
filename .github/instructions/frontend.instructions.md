---
applyTo: "frontend/**"
---

# Frontend guardrails

- Preserve the existing visual direction and component patterns.
- Design and verify changes mobile-first.
- Keep user-facing copy in Polish and consistent with the app tone.
- After changing login, navigation, matches, ranking, or profile flows, run Playwright smoke when possible.
- Prefer staging for E2E validation; use production only for smoke checks.
- Protect a11y:
  - semantic elements,
  - clear focus/disabled/error states,
  - proper form labels.
- Avoid UX regressions in critical flows:
  - login,
  - matches,
  - ranking,
  - profile,
  - admin.
