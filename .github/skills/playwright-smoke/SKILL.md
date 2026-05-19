# Playwright smoke (world-cup-typer)

## Cel
Szybka weryfikacja, czy aplikacja działa po zmianach: uruchomienie UI, ekran logowania, opcjonalnie logowanie kont testowych.

## Jak uruchomić lokalnie
```bash
cd frontend
npm ci
npx playwright install --with-deps chromium
E2E_BASE_URL=http://127.0.0.1:4173 npm run test:e2e:smoke
```

## Jak uruchamia się w CI
- Workflow: `.github/workflows/playwright-smoke.yml`
- Triggery: `pull_request`, `push` na `main`, `workflow_dispatch`
- Sekrety (repo settings):
  - `E2E_BASE_URL`
  - `E2E_ADMIN_EMAIL`
  - `E2E_ADMIN_PASSWORD`
  - `E2E_PLAYER_EMAIL`
  - `E2E_PLAYER_PASSWORD`

## Gdzie szukać logów i artefaktów
- GitHub Actions → run `Playwright Smoke`.
- Na failure pobierz artifact `playwright-artifacts`:
  - trace (`trace.zip`),
  - screenshoty,
  - video.

## Interpretacja failure
- Błąd na teście „ładowanie logowania” zwykle oznacza niedostępny `E2E_BASE_URL` albo problem renderu frontendu.
- Błąd na teście logowania roli zwykle oznacza problem z danymi konta, auth API lub dostępnością backendu.
