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

## Backend
- Hosting docelowy: DigitalOcean App Platform, Azure App Service lub inny runtime dla obrazu Docker/.NET
- Dockerfile: `backend/WorldCupTyper.Api/Dockerfile`
- Port kontenera: `8080`
- Kluczowe zmienne:
  - `ASPNETCORE_ENVIRONMENT`
  - `ConnectionStrings__DefaultConnection`
  - `Jwt__Key`
  - `Jwt__Issuer`
  - `Jwt__Audience`
  - `Jwt__ExpiryMinutes`
  - `Cors__AllowedOrigins__0`
  - `Seed__Enabled=false`

## CI
Workflow `.github/workflows/ci.yml` uruchamia:
- backend restore/build/test,
- frontend `npm ci` + build,
- build obrazu API.

## Co jeszcze pozniej
- deployment workflow dla GitHub Pages,
- deployment workflow dla backendu,
- zarzadzanie sekretami srodowiskowymi w GitHub,
- automatyczne migracje bazy przy deployu albo osobny krok release.
