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

Przydatne dokumenty:
- `docs/deployment-prep.md`
- `docs/mvp-status.md`

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
Lokalny backend dopuszcza frontend uruchomiony zarówno pod `http://localhost:5173`, jak i `http://127.0.0.1:5173`.

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
- `DatabaseStartup__ApplyMigrationsOnStartup=true`
- `Seed__Enabled=false`

### Frontend environment variables
Docelowy frontend powinien dostać:
- `VITE_API_BASE_URL=https://api-typer.marekwozniak.me`

### Frontend on GitHub Pages
Repo zawiera workflow `.github/workflows/frontend-pages.yml`, który publikuje frontend na GitHub Pages po pushu do `main`.

Aby go uruchomić docelowo:
- w ustawieniach repo otwórz `Settings -> Pages`,
- ustaw `Source` na `GitHub Actions`,
- skonfiguruj custom domain `typer.marekwozniak.me`,
- ustaw DNS dla subdomeny zgodnie z konfiguracją GitHub Pages.

Build Pages używa `npm run build:pages`, które oprócz zwykłego buildu tworzy też `404.html` dla fallbacku SPA przy odświeżaniu podstron.

### Docker build for API
Obraz API da sie zbudowac z katalogu repo:

```bash
docker build -f backend/WorldCupTyper.Api/Dockerfile -t world-cup-typer-api .
```

### Backend image release
Repo zawiera workflow `.github/workflows/backend-image.yml`, który po zmianach w `backend/` publikuje obraz API do GitHub Container Registry.

Docelowa nazwa obrazu:

```text
ghcr.io/wozniakmarek/world-cup-typer-api
```

Przykladowe tagi:
- `latest` dla `main`,
- `sha-<short-commit>` dla konkretnego commita,
- `main` jako tag galezi.

### DigitalOcean App Platform
Repo zawiera tez:
- app spec `.do/app.yaml`,
- reczny workflow `.github/workflows/backend-app-platform.yml`.

Ten workflow:
1. buduje obraz API,
2. publikuje go do GHCR,
3. wdraza do DigitalOcean App Platform po konkretnym digestcie obrazu.

Do uruchomienia w GitHub trzeba dodac sekrety:
- `DIGITALOCEAN_ACCESS_TOKEN`
- `GHCR_CREDENTIALS` w formacie `username:token`
- `DEFAULT_CONNECTION`
- `JWT_KEY`

Repo zawiera tez reczny workflow `.github/workflows/backend-migrations.yml`, ktory uruchamia `dotnet ef database update` przeciwko connection stringowi z sekretu `DEFAULT_CONNECTION`.

### Health endpoint
Backend udostępnia anonimowe endpointy zdrowia:

```text
GET /health
GET /health/live
```

`/health/live` sprawdza, czy proces działa.
`/health` jest readiness checkiem i obejmuje też połączenie z bazą.

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
- finalne spięcie domen `typer.marekwozniak.me` oraz `api-typer.marekwozniak.me`,
- dopiecie produkcyjnego deployu po skonfigurowaniu sekretow i zasobow hostingu,
- decyzja, czy docelowo zostawiamy migracje przy starcie aplikacji, czy przechodzimy w pelni na osobny workflow migracyjny.
