# Local Development

## Wymagania
- .NET SDK 8+
- Node.js 20+
- Docker Desktop

## Backend
1. `docker compose up -d`
2. `cd backend`
3. `dotnet restore`
4. `dotnet build`
5. `dotnet test`
6. `dotnet tool run dotnet-ef database update --project WorldCupTyper.Infrastructure --startup-project WorldCupTyper.Api`
7. `dotnet run --project WorldCupTyper.Api`

Domyślne API działa na `http://localhost:5000`.

## Frontend
1. `cd frontend`
2. `npm install`
3. skopiuj `.env.example` do `.env`
4. `npm run dev`

Frontend oczekuje `VITE_API_BASE_URL=http://localhost:5000`.

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

## CI
Workflow `.github/workflows/ci.yml` odtwarza podstawowa weryfikacje projektu:
- backend restore/build/test,
- frontend `npm ci` + build,
- build obrazu Docker dla API.
