# Local Development

## Wymagania
- .NET SDK 8+
- Node.js 20+
- Docker Desktop

## Szybki start
Z katalogu repo mozesz uzyc lokalnego launchera:

```powershell
.\typer.ps1 start
```

Skrot uruchamia PostgreSQL, API na `http://localhost:5000` i frontend na `http://localhost:5173`.

Dostepne komendy:
- `.\typer.ps1 start` - start lokalnego stacka i otwarcie strony,
- `.\typer.ps1 stop` - zatrzymanie lokalnych procesow projektu i Postgresa,
- `.\typer.ps1 build` - restore/build/test backendu oraz build frontendu,
- `.\typer.ps1 rebuild` - stop, build i ponowny start,
- `.\typer.ps1 status` - status portow i healthcheckow.

Na Windows mozesz tez uzyc wrappera:

```powershell
.\typer.cmd start
```

## Backend
1. `docker compose up -d`
2. `cd backend`
3. `dotnet restore`
4. `dotnet build`
5. `dotnet test`
6. `dotnet tool run dotnet-ef database update --project WorldCupTyper.Infrastructure --startup-project WorldCupTyper.Api`
7. `dotnet run --project WorldCupTyper.Api`

Domyślne API działa na `http://localhost:5000`.

### football-data.org automation

Automatyczny import terminarza i wynikow jest domyslnie wylaczony. Do lokalnego lub stagingowego testu ustaw konfiguracje przez zmienne srodowiskowe albo sekrety hostingu:

```powershell
$env:FootballData__Enabled = "true"
$env:FootballData__ApiToken = "<football-data-token>"
$env:FootballData__CompetitionCode = "WC"
$env:FootballData__SyncIntervalMinutes = "30"
$env:FootballData__SettleAutomatically = "true"
```

Worker uruchamia synchronizacje tylko wtedy, gdy `FootballData__Enabled=true` i token nie jest pusty. Ten sam import mozna wywolac recznie jako admin przez `POST /api/admin/matches/sync-football-data`, co jest zalecane do smoke testu na stagingu przed wlaczeniem cyklicznej automatyzacji.

Tokena API nie zapisujemy w repozytorium. Zmiana produkcyjnej konfiguracji importu lub operacje na realnych danych wymagaja jawnej decyzji czlowieka i powinny byc poprzedzone staging smoke.

## Frontend
1. `cd frontend`
2. `npm install`
3. skopiuj `.env.example` do `.env`
4. `npm run dev`

Frontend oczekuje `VITE_API_BASE_URL=http://localhost:5000`.
Domyslny CORS backendu wspiera lokalny frontend pod `http://localhost:5173` oraz `http://127.0.0.1:5173`.

## Seed development
- Admin: `admin@marekwozniak.me` / `ChangeMe123!`
- Przykładowi gracze: `marek@typer.local`, `kuba@typer.local`, `bartek@typer.local`, `pawel@typer.local`, `asia@typer.local`
- Hasło przykładowych graczy: `ChangeMe123!`

Wartości w `appsettings.Development.json` są tylko do developmentu lokalnego i muszą zostać zastąpione w środowisku docelowym.

## Kontener API
Z katalogu repo mozna zbudowac obraz backendu:

```bash
docker build -f backend/WorldCupTyper.Api/Dockerfile -t world-cup-typer-api .
```

Kontener nasluchuje na porcie `8080`.

Po uruchomieniu API mozna sprawdzic prosty stan aplikacji:

```bash
curl http://localhost:5000/health
curl http://localhost:5000/health/live
```

`/health/live` sprawdza sam proces aplikacji, a `/health` obejmuje tez gotowosc z baza danych.

## CI
Workflow `.github/workflows/ci.yml` odtwarza podstawowa weryfikacje projektu:
- backend restore/build/test,
- frontend `npm ci` + build,
- build obrazu Docker dla API.
