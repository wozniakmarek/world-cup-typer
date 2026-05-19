# Deployment Preparation

## Cel
Repo jest przygotowane do kolejnego etapu wdrozenia, ale bez wymuszania konkretnego hostingu. Aktualny stan daje:
- CI w GitHub Actions,
- kontenerowalny backend API,
- konfiguracje oparte o zmienne srodowiskowe,
- CORS gotowy do lokalnego frontendu i docelowej subdomeny.

## Frontend
- Hosting docelowy: GitHub Pages lub inny statyczny hosting pod `typer.marekwozniak.me`
- Kluczowa zmienna: `VITE_API_BASE_URL=https://api-typer.marekwozniak.me`
- Vite ma `base: "/"`, wiec jest gotowy pod osobna subdomene.
- Workflow deployu: `.github/workflows/frontend-pages.yml`
- Build Pages: `npm run build:pages`
- Build Pages tworzy `dist/404.html`, zeby deep linki React Router dzialaly po odswiezeniu podstrony na GitHub Pages.
- Custom domain dla workflow GitHub Actions trzeba ustawic w `Settings -> Pages`, a nie przez plik `CNAME`.

## Backend
- Hosting docelowy: DigitalOcean App Platform, Azure App Service lub inny runtime dla obrazu Docker/.NET
- Dockerfile: `backend/WorldCupTyper.Api/Dockerfile`
- Workflow obrazu: `.github/workflows/backend-image.yml`
- Workflow deployu do App Platform: `.github/workflows/backend-app-platform.yml`
- App spec: `.do/app.yaml`
- Rejestr obrazu: `ghcr.io/wozniakmarek/world-cup-typer-api`
- Port kontenera: `8080`
- Health endpoints: `/health` i `/health/live`
- Kluczowe zmienne:
  - `ASPNETCORE_ENVIRONMENT`
  - `ConnectionStrings__DefaultConnection`
  - `Jwt__Key`
  - `Jwt__Issuer`
  - `Jwt__Audience`
  - `Jwt__ExpiryMinutes`
  - `Cors__AllowedOrigins__0`
  - `DatabaseStartup__ApplyMigrationsOnStartup`
  - `Seed__Enabled=false`
  - `FootballData__Enabled=false`
  - `FootballData__ApiToken`
  - `FootballData__CompetitionCode=WC`
  - `FootballData__SyncIntervalMinutes=30`
  - `FootballData__SettleAutomatically=true`

## CI
Workflow `.github/workflows/ci.yml` uruchamia:
- backend restore/build/test,
- frontend `npm ci` + build,
- build obrazu API.

Workflow `.github/workflows/backend-image.yml` publikuje gotowy obraz backendu do GHCR z tagami:
- `latest` dla domyslnej galezi,
- `sha-<commit>`,
- nazwa galezi.

Przykladowe uruchomienie obrazu lokalnie po pobraniu z rejestru:

```bash
docker run --rm -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="<postgres-connection-string>" \
  -e Jwt__Key="<jwt-secret>" \
  -e Jwt__Issuer="WorldCupTyper" \
  -e Jwt__Audience="WorldCupTyper.Client" \
  -e Seed__Enabled=false \
  ghcr.io/wozniakmarek/world-cup-typer-api:latest
```

## DigitalOcean App Platform
Workflow deployu do DigitalOcean jest reczny (`workflow_dispatch`), zeby nie uruchamiac produkcyjnego wdrozenia przez przypadek po kazdym pushu.

Wymagane sekrety repo:
- `DIGITALOCEAN_ACCESS_TOKEN`
- `GHCR_CREDENTIALS`
- `DEFAULT_CONNECTION`
- `JWT_KEY`
- `FOOTBALL_DATA_API_TOKEN`

`GHCR_CREDENTIALS` powinno miec format `username:token`. Zgodnie z dokumentacja DigitalOcean App Platform i `digitalocean/app_action`, sekret ten trafia do `registry_credentials` w app specu i pozwala App Platform pobrac prywatny obraz z GHCR.

Workflow buduje obraz, publikuje go do GHCR i przekazuje jego digest do `.do/app.yaml` przez zmienna `API_IMAGE_DIGEST`, dzieki czemu wdrazany jest dokladnie ten artefakt, ktory zostal zbudowany w danym runie.

Jesli chcesz przejac migracje bazy poza startup aplikacji, mozna ustawic `DatabaseStartup__ApplyMigrationsOnStartup=false` i uruchamiac migracje osobnym krokiem release.

## football-data.org rollout

Automatyczny worker football-data.org powinien zostac wlaczony dopiero po smoke tescie na stagingu.

Domyslna wartosc w `.do/app.yaml` to:

```yaml
FootballData__Enabled=false
FootballData__CompetitionCode=WC
FootballData__SyncIntervalMinutes=30
FootballData__SettleAutomatically=true
```

Token musi byc ustawiony jako sekret `FOOTBALL_DATA_API_TOKEN`; nie zapisujemy go w repozytorium ani w jawnych wartosciach app specu.

Zalecana kolejnosc:

1. Dodaj `FOOTBALL_DATA_API_TOKEN` w GitHub Secrets.
2. Wykonaj deploy backendu z `FootballData__Enabled=false`.
3. Uruchom migracje bazy albo zostaw `DatabaseStartup__ApplyMigrationsOnStartup=true`.
4. Zaloguj sie jako admin na stagingu.
5. Wywolaj recznie `POST /api/admin/matches/sync-football-data`.
6. Sprawdz, czy import nie utworzyl duplikatow druzyn ani meczow i czy zakonczone mecze z wynikiem po 90 minutach zostaly rozliczone.
7. Dopiero po pozytywnym smoke ustaw `FootballData__Enabled=true` w srodowisku stagingowym.
8. Produkcyjne wlaczenie workera wymaga osobnej decyzji czlowieka.

## Osobny workflow migracyjny
Repo zawiera tez reczny workflow `.github/workflows/backend-migrations.yml`.

Ten workflow:
- ustawia .NET 8,
- przywraca lokalne narzedzie `dotnet-ef`,
- uruchamia `dotnet ef database update`,
- korzysta z sekretu `DEFAULT_CONNECTION`.

To najprostsza sciezka, jesli chcesz rozdzielic:
1. migracje bazy,
2. deploy nowej wersji API.

## Co jeszcze pozniej
- zarzadzanie sekretami srodowiskowymi w GitHub,
- automatyczne migracje bazy przy deployu albo osobny krok release.
