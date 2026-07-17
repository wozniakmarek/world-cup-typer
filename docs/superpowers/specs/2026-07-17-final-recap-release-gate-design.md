# Final Recap Release Gate Design

## Cel

Finalny recap może zostać zmergowany i wdrożony przed końcem turnieju, ale nie może pokazać tabeli, ciekawostek ani personalnego recapu przed rozliczeniem finału Argentyna-Hiszpania i domknięciem pełnego zestawu meczów.

## Podejście

Wybieramy bramkę danych zamiast deployu sterowanego czasem. Backend publikuje publiczny endpoint dostępności, a endpointy z recapem zwracają dane dopiero wtedy, gdy finalny mecz jest rozliczony, wszystkie wymagane mecze są rozliczone, wyniki typów są policzone i istnieją snapshoty rankingu dla finału. Frontend pokazuje wtedy normalny recap; wcześniej pokazuje spokojny ekran "recap odblokuje się po rozliczeniu finału".

## Kontrakt

`GET /api/summary/final/availability` jest publiczny i zwraca:

- `isReady`
- `reason`
- `settledMatchesCount`
- `requiredSettledMatchesCount`
- `totalMatchesCount`
- `finalMatchLabel`

`GET /api/summary/final` i `GET /api/summary/final/me` pozostają istniejącymi źródłami finalnych danych, ale przed gotowością zwracają błąd konfliktu zamiast częściowego recapu.

## Warunki Gotowości

Recap jest gotowy, gdy:

- istnieje mecz finałowy Argentyna-Hiszpania, rozpoznawany po kodach drużyn `ARG` i `ESP`,
- ten mecz ma `IsSettled = true`,
- liczba rozliczonych meczów spełnia wymagany próg, domyślnie `104`,
- nie ma nierozliczonych meczów w harmonogramie,
- finalny mecz ma policzone wyniki typów,
- finalny mecz ma snapshoty leaderboardu.

## Frontend

Publiczny `/summary/final` i chroniony `/summary/final/me` najpierw pytają o dostępność. Gdy `isReady=false`, pokazują ten sam komunikat blokady dopasowany do layoutu recapu. Gdy `isReady=true`, dotychczasowy widok działa bez zmian: animowany ranking, ciekawostki turniejowe i personalne podsumowanie.

## Release

Bez laptopa po finale najbezpieczniejszy model to wcześniejszy deploy kodu z zamkniętą bramką. Po rozliczeniu finału w panelu/adminie lub przez istniejącą synchronizację danych recap otworzy się sam, bez ponownego merge, bez ręcznego workflow i bez produkcyjnego skryptu zapisującego dane.

## Testy

- backend: czerwone testy dla niedostępnego i dostępnego recapu,
- backend: test publicznego atrybutu endpointu availability,
- frontend smoke: publiczny i personalny recap pokazują blokadę, gdy API zwraca `isReady=false`,
- regresja: istniejące testy recapu dalej przechodzą dla `isReady=true`.
