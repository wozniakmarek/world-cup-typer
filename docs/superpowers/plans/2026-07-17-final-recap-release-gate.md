# Final Recap Release Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a production-safe data gate so the final recap can be deployed early and opens automatically only after the Argentina-Spain final and the full tournament are settled.

**Architecture:** Keep the gate in the summary backend service and expose a small public availability endpoint. The frontend asks availability first and renders either the locked state or the existing recap UI.

**Tech Stack:** ASP.NET Core 8, EF Core, xUnit/FluentAssertions, React, TanStack Query, Playwright smoke tests.

---

## File Structure

- `backend/WorldCupTyper.Application/DTOs/FinalSummaryDtos.cs`: add the availability DTO.
- `backend/WorldCupTyper.Application/Services/Interfaces/IFinalSummaryService.cs`: add the availability method.
- `backend/WorldCupTyper.Application/Services/FinalSummaryService.cs`: compute readiness and guard recap methods.
- `backend/WorldCupTyper.Api/Controllers/SummaryController.cs`: add the public availability route.
- `backend/WorldCupTyper.Tests/FinalSummaryServiceTests.cs`: add red/green readiness tests.
- `backend/WorldCupTyper.Tests/SummaryControllerAuthorizationTests.cs`: assert the new route is public.
- `frontend/src/api/types.ts`: add the availability type.
- `frontend/src/api/services.ts`: add `summaryApi.getFinalAvailability`.
- `frontend/src/features/summary/FinalSummaryLockedState.tsx`: shared locked state.
- `frontend/src/features/summary/FinalSummaryPage.tsx`: availability-first public recap.
- `frontend/src/features/summary/PersonalFinalSummaryPage.tsx`: availability-first personal recap.
- `frontend/e2e/smoke.spec.ts`: add local smoke coverage for the locked state and keep ready-state mocks green.

### Task 1: Backend Availability Contract

- [ ] **Step 1: Write failing DTO/controller tests**

Add assertions that `SummaryController.GetFinalSummaryAvailability` exists, has `AllowAnonymousAttribute`, and returns `FinalSummaryAvailabilityDto` fields.

- [ ] **Step 2: Run red test**

Run: `dotnet test backend/WorldCupTyper.Tests/WorldCupTyper.Tests.csproj --no-restore --filter "FullyQualifiedName~SummaryControllerAuthorizationTests"`

Expected: fail because the method and DTO do not exist.

- [ ] **Step 3: Add minimal DTO/interface/controller route**

Create `FinalSummaryAvailabilityDto` and `IFinalSummaryService.GetFinalSummaryAvailabilityAsync`, then route `GET api/summary/final/availability`.

- [ ] **Step 4: Run green test**

Run the same filtered command. Expected: pass.

### Task 2: Backend Readiness Rules And Guard

- [ ] **Step 1: Write failing service tests**

Add one test where ARG-ESP is unsettled and `IsReady=false`; add one test where ARG-ESP is settled, required match count is met, results and snapshots exist, and `IsReady=true`.

- [ ] **Step 2: Run red test**

Run: `dotnet test backend/WorldCupTyper.Tests/WorldCupTyper.Tests.csproj --no-restore --filter "FullyQualifiedName~FinalSummaryServiceTests"`

Expected: fail because readiness rules are missing.

- [ ] **Step 3: Implement minimal readiness logic**

Use active users, matches, predictions/results, and leaderboard snapshots already in `IAppDbContext`. Guard `GetFinalSummaryAsync` and `GetPersonalFinalSummaryAsync` by throwing a conflict exception until ready.

- [ ] **Step 4: Run green test**

Run the same filtered command. Expected: pass.

### Task 3: Frontend Locked State

- [ ] **Step 1: Write failing smoke tests**

Mock `/api/summary/final/availability` as unavailable and assert `/summary/final` plus `/summary/final/me` show the locked message and do not render the chart.

- [ ] **Step 2: Run red smoke**

Run: `cd frontend; npm run test:e2e:smoke -- --grep "recap jest jeszcze zablokowany"`

Expected: fail because the frontend does not call availability.

- [ ] **Step 3: Implement availability-first UI**

Add `FinalSummaryLockedState`; query availability before summary queries; enable summary queries only when ready.

- [ ] **Step 4: Run green smoke**

Run the same filtered smoke command. Expected: pass.

### Task 4: Full Verification

- [ ] **Step 1: Backend tests**

Run: `dotnet test backend/WorldCupTyper.sln --no-restore`

- [ ] **Step 2: Frontend checks**

Run: `cd frontend; npm run lint; npm run build`

- [ ] **Step 3: Targeted smoke**

Run the existing final summary smoke grep plus the new locked tests.

- [ ] **Step 4: Commit**

Commit the completed code with a focused message after all checks pass.
