---
applyTo: "backend/**"
---

# Backend guardrails

- Input validation must exist on the API side, not only in UI.
- Every change to business rules (`3/1/0`, kickoff lock, predictions visibility, ranking order) needs a test.
- Treat auth, ranking, prediction/match contracts, and admin flows as high-regression-risk areas.
- Do not hardcode secrets, connection strings, or JWT keys.
- Keep DTOs and API contracts compatible with the current frontend behavior unless the task explicitly changes the contract.
- Be conservative with auth/authorization changes and prefer safer defaults.
