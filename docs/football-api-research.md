# Research API pilkarskiego do terminarza i rozliczen

Data researchu: 2026-05-19

## Cel

Celem jest wybranie darmowego albo taniego zrodla danych, ktore moze zasilac `world-cup-typer` terminarzem, statusem meczu i wynikiem po 90 minutach. To nie jest plan pelnej implementacji, tylko decyzja architektoniczna i etapowy kierunek wdrozenia.

## Rekomendacja

Najlepszym pierwszym kierunkiem jest `football-data.org`.

Powody:
- ma darmowy plan z World Cup w free tierze, terminarzem, opoznionymi wynikami i limitem 10 requestow/minute;
- tani upgrade `Free w/ Livescores` kosztuje obecnie 12 EUR/mies. i daje live scores oraz 20 requestow/minute;
- dokumentacja v4 ma pole `score.regularTime`, ktore bezposrednio odpowiada naszemu `Match.HomeScore90` / `Match.AwayScore90`;
- model obecnej aplikacji juz ma `Match.ExternalId`, `HomeScore90`, `AwayScore90`, `HomeScoreFinal`, `AwayScoreFinal`, `WinnerTeamId`, `IsSettled` i stub `IScheduleImportService`, wiec integracja nie wymaga przebudowy domeny.

Plan B to `API-Football` od API-Sports. Jest bardziej rozbudowane, darmowy plan ma 100 requestow/dzien, a najtanszy platny plan daje 7500 requestow/dzien za 19 USD/mies. To dobry fallback, jesli `football-data.org` okaze sie opoznione za bardzo albo nie pokryje konkretnego sezonu/endpointu World Cup tak, jak potrzebujemy.

Nie rekomenduje startu od Sportmonks ani dedykowanego World Cup API: maja lepsze pokrycie produktowe, ale koszt jest wyraznie za wysoki dla obecnego zakresu MVP.

## Porownanie kandydatow

| Dostawca | Koszt i limit | Terminarz | Status/live | Wynik po 90 minutach | Ocena dla MVP |
| --- | --- | --- | --- | --- | --- |
| football-data.org | Free: 0 EUR, 10 req/min, 12 rozgrywek; live: 12 EUR/mies., 20 req/min | Tak, World Cup jest w free tierze | Free ma opoznione scores/schedules; live w tanim planie | Tak, `score.regularTime` od v4; dla zwyklych meczow mozna uzyc `score.fullTime` | Najlepszy start |
| API-Football / API-Sports | Free: 100 req/dzien; Pro: 19 USD/mies., 7500 req/dzien | Tak, wszystkie rozgrywki i endpointy w planach | Tak, livescore i statusy, aktualizacje live deklarowane przez dostawce | Tak, rozbicie `score` na halftime/fulltime/extratime/penalty | Najlepszy fallback |
| TheSportsDB | Free: 30 req/min; premium: 9 USD/mies. z 2-min livescore | Tak, ale dane sa bardziej community/content-oriented | Livescore dopiero w premium | `ft_score` istnieje w livescore, ale nie budowalbym na tym settlementu bez pilota | Tani eksperyment, nie glowny feed |
| Sportmonks | Starter od 29 EUR/mies.; World Cup widgets od 78 EUR/mies.; API World Cup od 69 EUR/mies. | Tak | Tak, bogate live data i events | Tak, score types rozrozniaja 90 min, dogrywke i karne | Dobre technicznie, za drogie na start |
| WorldCupAPI.com | 499 EUR/mies., 200k req/dzien | Tak | Tak, dedykowane dane World Cup | Tak, feed ma `ft_score`, `et_score`, `ps_score` | Odrzucic kosztowo |

## Dopasowanie do obecnego modelu

Obecny `Match` jest blisko gotowy pod integracje:
- `ExternalId` moze przechowywac id meczu z dostawcy;
- `KickoffTimeUtc`, `Venue`, `Status`, `Phase`, `GroupName` pokrywaja podstawowy terminarz;
- `HomeScore90` i `AwayScore90` sa kluczowe dla zasad typowania;
- `HomeScoreFinal`, `AwayScoreFinal` i `WinnerTeamId` pozwalaja pokazac wynik pucharowy bez mieszania go z punktacja za typ po 90 minutach;
- `IsSettled` i `SettledAtUtc` zabezpieczaja idempotencje settlementu;
- `LeaderboardSnapshot` juz wspiera przyszly wykres progresu po meczu.

Jedyna luka modelowa przed produkcyjna integracja to brak zewnetrznych identyfikatorow na `Team`. Przy jednym providerze mozna mapowac po `CountryCode` i nazwie, ale bezpieczniej bedzie dodac osobna tabele mapowania, np. `ExternalTeamMapping(Provider, ExternalTeamId, TeamId, DisplayName)`. Dla `Match.ExternalId` warto przyjac format z prefiksem, np. `football-data:123456`, zeby pozniej nie blokowac zmiany dostawcy.

## Automatyzacja po obszarach

### Import terminarza

Mozemy zautomatyzowac w pierwszym etapie. `IScheduleImportService` powinien pobierac mecze World Cup, mapowac `ExternalId`, `MatchNumber`, faze, grupe, gospodarza, goscia, kickoff UTC i venue, a nastepnie robic upsert. Dla faz pucharowych trzeba zachowac obecne pola `HomeSlotRule` / `AwaySlotRule`, bo przed rozstrzygnieciem grup czesc slotow moze byc znana jako "1A vs 2B", a nie jako konkretna druzyna.

Rekomendowana czestotliwosc:
- przed turniejem: raz dziennie;
- w dni meczowe: co 15-30 minut dla meczow dzisiejszych i jutrzejszych;
- po recznych zmianach FIFA: reczny przycisk admin/import jako override.

### Blokada po kickoffie

Juz dziala domenowo przez `Match.CanAcceptPredictions(nowUtc)`, wiec nie trzeba czekac na status live z API. Feed powinien tylko aktualizowac `KickoffTimeUtc`, a blokada powinna opierac sie na lokalnym czasie UTC z `IDateTimeProvider`. Status API moze pomoc w UI, ale nie powinien byc jedynym mechanizmem blokady.

### Wynik po 90 minutach

To jest najwazniejszy punkt. Dla `football-data.org` nalezy mapowac:
- jesli `score.regularTime` istnieje, zapisac je do `HomeScore90` / `AwayScore90`;
- jesli `duration == REGULAR`, mozna zapisac `score.fullTime` jako wynik po 90 minutach;
- jesli `duration` oznacza dogrywke albo karne i nie ma `regularTime`, nie rozliczac automatycznie.

Dla `API-Football` analogicznie trzeba uzywac `score.fulltime`, a `score.extratime` i `score.penalty` traktowac jako dane dodatkowe do wyniku finalnego.

### Settlement

Settlement mozna zautomatyzowac dopiero po zapisaniu wyniku po 90 minutach i statusie finalnym z feedu. Job powinien:
- pobrac zakonczone mecze bez `IsSettled`;
- uzupelnic `HomeScore90` / `AwayScore90`;
- opcjonalnie uzupelnic wynik finalny i `WinnerTeamId`;
- wywolac `MatchSettlementService.SettleMatchAsync(match.Id)`;
- byc idempotentny i logowac pominiecia, np. brak `regularTime` w meczu pucharowym.

Warto zostawic reczny endpoint admina jako fallback, bo dane sportowe potrafia miec korekty po meczu.

### Ranking i przyszly wykres

Ranking po settlementcie juz jest spiety przez `LeaderboardBuilder` i `LeaderboardSnapshot`. Po automatyzacji settlementu wykres progresu mozna budowac bez osobnej integracji z API: snapshot po meczu wystarczy jako punkt osi czasu.

## Etapowy plan wdrozenia

1. Pilot bez sekretow w repo

   Utworzyc konto `football-data.org`, dodac klucz jako sekret srodowiskowy, sprawdzic endpoint World Cup i jeden przykladowy mecz zakonczony dogrywka/karnymi. Kryterium przejscia: potwierdzone `score.regularTime` albo jasny fallback.

2. Adapter infrastruktury

   Zastapic `StubScheduleImportService` implementacja `FootballDataScheduleImportService`. Dodac opcje konfiguracyjne: base URL, token, competition code, dry-run, lookback/lookahead. Nie logowac tokena.

3. Import terminarza

   Zrobic upsert meczow po `ExternalId`. Na start aktualizowac tylko pola bezpieczne: kickoff, venue, status, pary druzyn, faze, grupe i slot rules. Dodac testy mapowania statusow oraz wynikow.

4. Import wynikow i statusow

   Dodac osobny serwis/job dla wynikow, ktory aktualizuje tylko mecze rozpoczete lub zakonczone. Nie rozliczac meczu, jesli brakuje wyniku po 90 minutach.

5. Automatyczny settlement

   Po udanym imporcie finalnego wyniku wywolac settlement dla nierozliczonych meczow. Dodac metryke/log: imported, skipped, settled, failed.

6. Produkcyjna ostroznosc

   Domyslnie staging-first. Produkcja: najpierw dry-run i lekki smoke. Kazda zmiana danych produkcyjnych wymaga jawnej decyzji czlowieka, zgodnie z AGENTS.md.

## Decyzje techniczne do podjecia przed implementacja

- Czy trzymamy tylko `Match.ExternalId` z prefiksem providera, czy dodajemy osobna tabele `ExternalMatchMapping`.
- Czy dodajemy `ExternalTeamMapping`, zeby uniknac problemow z nazwami typu `USA`, `United States`, `United States of America`.
- Czy status `Finished` ma oznaczac "wynik pobrany, ale jeszcze nierozliczony", a `Settled` pozostaje obecnym stanem po naliczeniu punktow. To pasuje do aktualnego enumu.
- Czy live status jest potrzebny w UI w MVP. Dla samej blokady typow wystarczy lokalny kickoff UTC.

## Zrodla

- football-data.org pricing: https://www.football-data.org/pricing
- football-data.org coverage: https://www.football-data.org/coverage
- football-data.org scores/overtime docs: https://docs.football-data.org/general/v4/overtime.html
- API-Football pricing: https://www.api-football.com/pricing
- API-Sports football overview: https://api-sports.io/sports/football
- API-Football beginner guide: https://www.api-football.com/news/post/how-to-get-started-with-api-football-the-complete-beginners-guide
- TheSportsDB pricing: https://www.thesportsdb.com/docs_pricing
- TheSportsDB docs: https://www.thesportsdb.com/documentation
- Sportmonks pricing: https://www.sportmonks.com/football-api/plans-pricing/
- Sportmonks score types: https://docs.sportmonks.com/v3/tutorials-and-guides/tutorials/includes/scores
- WorldCupAPI pricing: https://worldcupapi.com/pricing
