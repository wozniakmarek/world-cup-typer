# Copilot instructions dla `world-cup-typer`

## Architektura monorepo
- `frontend/` — React + Vite + TypeScript (UI/PWA).
- `backend/` — .NET 8 Web API (auth, reguły typowania, ranking, admin).
- `docs/` — dokumentacja produktu i deployu.

## Komendy build/test
- Backend:
  - `cd backend && dotnet restore`
  - `cd backend && dotnet build WorldCupTyper.sln --configuration Release --no-restore`
  - `cd backend && dotnet test WorldCupTyper.sln --configuration Release --no-build`
- Frontend:
  - `cd frontend && npm ci`
  - `cd frontend && npm run build`
  - `cd frontend && npm run lint`
  - `cd frontend && npm run test:e2e:smoke` (Playwright smoke)

## Reguły biznesowe (nie łamać)
- Punktacja typów: **3/1/0**:
  - 3 pkt za dokładny wynik po 90 minutach,
  - 1 pkt za poprawnie trafiony wynik meczu (1X2),
  - 0 pkt w pozostałych przypadkach.
- Typy są edytowalne tylko **przed kickoffem**; po kickoffie backend musi blokować zapis/edycję.
- Widoczność typów innych graczy jest dostępna dopiero **po kickoffie**.

## Guardrails bezpieczeństwa i procesu
- Nie zapisuj sekretów/tokenów/hasła w kodzie, testach, workflow ani dokumentacji.
- Nie osłabiaj istniejących workflow deployowych ani konfiguracji env.
- Zmiany rób minimalne i kompatybilne wstecz.
- Nie merguj PR bez zielonych checków CI i review.
