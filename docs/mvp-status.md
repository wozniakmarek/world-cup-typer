# MVP Status

Stan projektu na 2026-05-18.

## Cel
Ten dokument porownuje poczatkowe zalozenia projektu z aktualnym stanem repo. Ma ulatwic dalsze planowanie bez wracania za kazdym razem do calej historii ustalen.

## Zrobione

### Backend
- JWT auth dla `Admin` i `Player`
- endpointy `auth`, `teams`, `matches`, `predictions`, `ranking`
- endpointy admina dla graczy, druzyn i meczow
- walidacja typowania po kickoffie po stronie backendu
- scoring `3/1/0`
- rozliczanie meczow
- ranking i `LeaderboardSnapshot`
- seed development
- migracje EF Core
- health endpoints `/health` i `/health/live`
- konfigurowalne migracje przy starcie przez `DatabaseStartup__ApplyMigrationsOnStartup`

### Frontend
- login
- dashboard
- lista meczow z filtrami
- szczegoly meczu z formularzem typu
- ranking
- profil gracza
- panel admina dla graczy
- panel admina dla druzyn
- panel admina dla meczow
- PWA-ready konfiguracja z manifestem i service workerem

### Operacyjne przygotowanie repo
- `docker-compose.yml` dla Postgresa
- `Dockerfile` dla API
- CI workflow build/test
- workflow deployu frontendu na GitHub Pages
- workflow publikacji obrazu backendu do GHCR
- workflow migracyjny EF Core
- template deployu backendu do DigitalOcean App Platform

## Zamkniete kryteria MVP
- Admin moze sie zalogowac.
- Admin moze dodac gracza.
- Gracz moze sie zalogowac.
- Admin moze dodac druzyny i mecze.
- Gracz widzi liste meczow.
- Gracz moze obstawic wynik meczu przed kickoffem.
- Gracz moze edytowac typ przed kickoffem.
- Backend blokuje dodanie i edycje typu po kickoffie.
- Przed kickoffem gracz nie widzi typow innych.
- Po kickoffie gracz widzi typy innych.
- Admin moze wpisac wynik po 90 minutach.
- Admin moze rozliczyc mecz.
- System przyznaje punkty `3/1/0` zgodnie z zasadami.
- Ranking pokazuje punkty i podstawowe statystyki.
- Gracz widzi swoje statystyki.
- Projekt uruchamia sie lokalnie wedlug README.
- Sa testy jednostkowe dla scoringu i kluczowych reguł biznesowych.

## Czesciowo domkniete
- Responsywnosc mobilna jest sensowna, ale adminowe ekrany nadal warto przejrzec stricte pod telefon i male viewporty.
- Deploy jest przygotowany workflowami i konfiguracja, ale nie zostal jeszcze skonfigurowany sekretami ani odpalony na docelowej infrastrukturze.
- PWA jest gotowe technicznie do instalacji, ale bez pelnych push notifications i bez rozbudowanego offline.

## Swiadomie odlozone poza MVP
- automatyczne pobieranie terminarza
- automatyczne pobieranie wynikow
- smart knockout resolver
- mailowe resety hasla i zaproszenia
- OAuth / Google login
- pelne web push notifications
- rozbudowane wykresy i zaawansowana analityka

## Najwazniejsze rzeczy do zrobienia dalej

### 1. Realny deploy
- skonfigurowac sekrety GitHub
- wlaczyc GitHub Pages
- skonfigurowac DNS dla `typer.marekwozniak.me`
- skonfigurowac hosting backendu i polaczenie z baza

### 2. Decyzja o migracjach produkcyjnych
- zostawic `DatabaseStartup__ApplyMigrationsOnStartup=true`
- albo przejsc na model: najpierw workflow migracyjny, potem deploy API

### 3. UX i polish MVP
- przejrzec adminowe formularze i listy pod telefon
- dodac bardziej czytelne empty states i loading states tam, gdzie sa jeszcze surowe
- dopracowac komunikaty bledu i sukcesu w kilku formularzach

### 4. Pierwszy etap po MVP
- wykres progresu punktow na bazie `LeaderboardSnapshot`
- import terminarza
- automatyczne wyniki
- push notifications

## Ostatnia potwierdzona weryfikacja
- `dotnet build WorldCupTyper.sln`
- `dotnet test WorldCupTyper.sln`
- `npm run build`
- `npm run build:pages`
- `docker build -f backend/WorldCupTyper.Api/Dockerfile -t world-cup-typer-api .`
- `dotnet tool run dotnet-ef database update --project backend/WorldCupTyper.Infrastructure`
- runtime check `/health` i `/health/live`
