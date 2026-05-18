# Typer Mistrzostw Świata

Prywatne MVP aplikacji webowej/PWA do typowania wyników meczów Mistrzostw Świata w grupie znajomych. Projekt jest przygotowany lokalnie, ale architektura jest już rozbita tak, żeby później bez bólu dodać deploy, powiadomienia push, import terminarza i bardziej rozbudowane statystyki.

## Stack
- Frontend: React, Vite, TypeScript, Tailwind CSS, React Router, TanStack Query, vite-plugin-pwa
- Backend: .NET 8, ASP.NET Core Web API, EF Core, JWT, PostgreSQL
- Baza: PostgreSQL w Docker Compose

## Struktura
- `frontend/`
- `backend/`
- `docs/`

## Co działa w MVP
- logowanie admina i graczy,
- zarządzanie graczami przez admina,
- zarządzanie drużynami i meczami przez admina,
- wpisywanie oraz edycja typów przed kickoffem,
- blokada typów po kickoffie po stronie backendu,
- widoczność typów innych graczy dopiero po kickoffie,
- wpisywanie wyników po 90 minutach,
- rozliczanie meczu i ranking 3/1/0,
- profil gracza ze statystykami i historią typów,
- responsywny interfejs w ciemnym motywie,
- PWA-ready z manifestem i service workerem.

## Local start

### 1. PostgreSQL
```bash
docker compose up -d
```

### 2. Backend
```bash
cd backend
dotnet restore
dotnet build
dotnet test
dotnet tool run dotnet-ef database update --project WorldCupTyper.Infrastructure --startup-project WorldCupTyper.Api
dotnet run --project WorldCupTyper.Api
```

API startuje domyślnie pod `http://localhost:5000`.

### 3. Frontend
```bash
cd frontend
npm install
copy ..\\.env.example .env
npm run dev
```

Frontend czyta `VITE_API_BASE_URL=http://localhost:5000`.

## Seed development
- Admin: `admin@marekwozniak.me` / `ChangeMe123!`
- Gracze: `marek@typer.local`, `kuba@typer.local`, `bartek@typer.local`, `pawel@typer.local`, `asia@typer.local`
- Hasło przykładowych graczy: `ChangeMe123!`

`appsettings.Development.json` zawiera wyłącznie development-only konfigurację lokalną. Do środowisk docelowych trzeba podać osobne connection stringi i sekrety JWT przez env/secrets.

## Testy
Uruchomione testy obejmują:
- `ScoringService`
- reguły `PredictionService`
- sortowanie `RankingService`

## CI
Repo jest przygotowane pod GitHub Actions przez workflow `.github/workflows/ci.yml`.

Pipeline uruchamia:
- build i test backendu `.NET 8`,
- build frontendu `Vite`,
- build obrazu Docker dla `WorldCupTyper.Api`.

## Deploy preparation

### Backend environment variables
Docelowy backend powinien dostać co najmniej:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ExpiryMinutes`
- `Cors__AllowedOrigins__0=https://typer.marekwozniak.me`
- `Seed__Enabled=false`

### Frontend environment variables
Docelowy frontend powinien dostać:
- `VITE_API_BASE_URL=https://api-typer.marekwozniak.me`

### Docker build for API
Obraz API da sie zbudowac z katalogu repo:

```bash
docker build -f backend/WorldCupTyper.Api/Dockerfile -t world-cup-typer-api .
```

## Migracje
Narzędzie EF jest dodane lokalnie przez manifest `dotnet-tools.json`.

Przykład:
```bash
cd backend
dotnet tool run dotnet-ef migrations add SomeChange --project WorldCupTyper.Infrastructure --startup-project WorldCupTyper.Api
dotnet tool run dotnet-ef database update --project WorldCupTyper.Infrastructure --startup-project WorldCupTyper.Api
```

## Kolejne etapy
- push notifications,
- import terminarza i wyników,
- smart knockout resolver,
- wykres progresu punktów na bazie `LeaderboardSnapshot`,
- deploy na subdomeny `typer.marekwozniak.me` oraz `api-typer.marekwozniak.me`.
