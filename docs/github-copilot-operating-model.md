# GitHub Copilot - model pracy dla `world-cup-typer`

## Cel
GitHub Copilot ma pomagac w codziennej pracy nad repo, ale wedlug jasnych granic:
- staging first,
- production only for smoke,
- zmiany kodowe i testowe po stronie Copilota,
- sekrety, DNS, rulesety i decyzje produkcyjne po stronie czlowieka.

## Co warto zlecac Copilotowi
- poprawki UI i UX w `frontend/`,
- poprawki API, walidacji i testow w `backend/`,
- rozbudowe smoke testow Playwright,
- diagnoze czerwonych checkow GitHub Actions,
- przygotowanie PR, opisow zmian i checklist release,
- przeglad zmian pod katem regresji w logowaniu, typach, scoringu i rankingu.

## Czego nie zlecac Copilotowi jako akcji automatycznej
- wpisywania lub rotacji sekretow w UI,
- zmian DNS i domen,
- zmian rulesetow i ustawien repo, jesli wymagaja klikniecia w UI,
- hurtowych zmian danych produkcyjnych,
- merge do `main` przy czerwonych checkach.

## Domyslny podzial srodowisk
- `staging`
  - pelne smoke i E2E,
  - odtwarzanie bledow,
  - czestsze testy po pushu i PR.
- `production`
  - tylko lekkie smoke:
    - otwarcie strony,
    - logowanie,
    - ranking i mecze sie laduja,
    - API odpowiada na `/health`.

## Agenci i ich rola
- `frontend-polish`
  - do poprawy layoutu, mobile, copy, a11y.
- `backend-guard`
  - do zmian backendowych, auth, DTO, walidacji i testow domenowych.
- `qa-smoke`
  - do Playwright, logowania admin/gracz, artefaktow i reprodukcji bledow.
- `release-check`
  - do finalnej kontroli release readiness i blockerow.

## Typowe zadania dla Copilota
1. Bugfix
   - reprodukcja,
   - minimalna poprawka,
   - test lub smoke,
   - PR z opisem przyczyny i walidacji.
2. UI polish
   - poprawka widoku,
   - build frontendu,
   - smoke dla logowania lub kluczowej sciezki.
3. Backend rule change
   - zmiana kodu,
   - test jednostkowy,
   - sprawdzenie, czy frontend kontrakt nie zostal zlamany.
4. CI failure
   - analiza runa,
   - poprawka workflow lub kodu,
   - ponowny zielony przebieg.

## Prompty, ktore warto dawac Copilotowi
- `Fix this regression without changing API contracts.`
- `Add tests for kickoff lock and ranking tiebreakers.`
- `Investigate failing Playwright smoke and summarize root cause with the smallest safe fix.`
- `Polish this page for mobile without changing the visual direction of the app.`
- `Review this PR for regressions in auth, scoring, ranking, and admin flows.`

## Dzienna petla pracy
1. Rano
   - sprawdzenie `production smoke`,
   - sprawdzenie `health` API.
2. W ciagu dnia
   - development i testy na stagingu,
   - PR review,
   - poprawki CI.
3. Przed wpuszczeniem zmian
   - zielone checki,
   - krotki sanity check UX,
   - brak blockerow w release-check.
