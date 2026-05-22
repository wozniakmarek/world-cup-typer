# Production Database Backups

## Cel
Ten dokument opisuje bezpieczny plan backupow produkcyjnej bazy `world-cup-typer`, zanim do aplikacji trafia realne typy, wyniki i ranking.

Backupy zawieraja dane produkcyjne uzytkownikow. Ich wlaczenie, miejsce przechowywania, dostep oraz ewentualne kopiowanie poza dostawce bazy wymagaja jawnej decyzji czlowieka przed wdrozeniem.

## Zakres danych
Plan obejmuje produkcyjna baze PostgreSQL uzywana przez backend API:
- konta uzytkownikow i role,
- typy uzytkownikow,
- mecze, wyniki i rozliczenia,
- ranking i snapshoty rankingu,
- dane administracyjne wymagane do odtworzenia stanu aplikacji.

Backupy, dumpy, logi restore ani dane odtworzone z produkcji nie sa zapisywane w repozytorium.

## Decyzja domyslna
Docelowym dostawca produkcyjnej bazy jest DigitalOcean Managed PostgreSQL.

Decyzja operacyjna przed startem produkcyjnym:

1. Uzyc natywnych backupow DigitalOcean Managed PostgreSQL jako warstwy podstawowej.
2. Oprzec restore operacyjny o point-in-time recovery dostepne dla klastra.
3. Przyjac limit PITR DigitalOcean: odtworzenie do punktu w czasie w ostatnich 7 dniach.
4. Nie uruchamiac osobnych cyklicznych dumpow poza DigitalOcean, dopoki czlowiek nie zatwierdzi docelowego miejsca przechowywania, kluczy szyfrujacych, retencji i listy osob z dostepem.

Jesli wybrany dostawca lub plan bazy nie oferuje automatycznych backupow i sensownego restore, produkcyjny start jest zablokowany do czasu zmiany planu albo zatwierdzenia alternatywy z szyfrowanymi dumpami.

## Automatyczne backupy
Warstwa podstawowa:
- DigitalOcean Managed PostgreSQL,
- automatyczne backupi tworzone przez DigitalOcean raz dziennie,
- write-ahead logs utrzymywane przez DigitalOcean do PITR w ostatnich 7 dniach,
- restore wykonywany do nowego klastra, a nie bezposrednio na aktywny primary node,
- backupy szyfrowane w spoczynku i polaczenia szyfrowane SSL zgodnie z funkcjami DigitalOcean Managed Databases,
- dostep ograniczony do wlasciciela infrastruktury i osob zatwierdzonych do produkcyjnego utrzymania.

Przed zaproszeniem uzytkownikow trzeba potwierdzic w panelu dostawcy:
- plan bazy i region,
- czy automatyczne backupy sa wlaczone,
- czy PITR jest dostepny i wlaczony,
- retencje 7 dni dla backupow/PITR,
- kto ma uprawnienia do wykonania restore,
- czy konto/team nie ma limitu klastrow blokujacego testowy restore do nowego klastra.

## Dodatkowe szyfrowane dumpy
Szyfrowany dump cykliczny jest opcjonalna druga warstwa, a nie warunek pierwszego wdrozenia, jesli natywne backupy dostawcy spelniaja wymagania.

Wlaczenie dumpow wymaga osobnej decyzji czlowieka. Decyzja musi wskazac:
- docelowe miejsce przechowywania, np. prywatny bucket S3-compatible u dostawcy infrastruktury,
- wlasciciela klucza szyfrujacego,
- sposob rotacji klucza,
- retencje,
- liste osob z dostepem,
- miejsce uruchamiania joba backupowego,
- sposob monitorowania powodzenia i porazek.

Minimalne wymagania dla dumpow:
- `pg_dump` uruchamiany z bezpiecznego srodowiska, ktore nie loguje connection stringa,
- szyfrowanie pliku przed wyslaniem do storage,
- storage prywatny, bez publicznego listowania i publicznego odczytu,
- lifecycle policy usuwajaca stare dumpy zgodnie z retencja,
- test restore z dumpa przed uznaniem mechanizmu za gotowy.

## Retencja
Retencja dla natywnych backupow DigitalOcean Managed PostgreSQL:
- backupi klastra sa tworzone raz dziennie,
- backupi sa utrzymywane przez 7 dni,
- PITR jest ograniczony do ostatnich 7 dni.

Ta retencja spelnia minimalne wymaganie startowe. Jesli projekt potrzebuje retencji dluzszej niz 7 dni, trzeba osobno zatwierdzic dodatkowe szyfrowane dumpy albo inna warstwe backupowa.

Rekomendowana retencja dla dodatkowych dumpow, jesli zostana zatwierdzone:
- 7 dumpow dziennych,
- 4 dumpy tygodniowe,
- 3 dumpy miesieczne,
- natychmiastowe usuniecie dumpow starszych niz zatwierdzona retencja.

Retencja nie powinna byc wydluzana "na wszelki wypadek" bez powodu, bo backup zawiera dane uzytkownikow.

## Dostep i odpowiedzialnosc
Dostep do backupow produkcyjnych ma tylko minimalna grupa operacyjna zatwierdzona przez wlasciciela projektu.

Zasady:
- brak backupow i dumpow w GitHub, artefaktach CI i lokalnych katalogach projektu,
- brak sekretow backupowych w repozytorium,
- brak produkcyjnych dumpow na maszynach developerskich,
- dostep nadawany imiennie, bez wspoldzielonych kont,
- restore produkcyjny albo restore z produkcyjnych danych wymaga decyzji czlowieka,
- restore na stagingu moze byc wykonany tylko wtedy, gdy zaakceptowano ryzyko danych produkcyjnych albo dane zostana zanonimizowane.

## Procedura testowego restore
Test restore trzeba wykonac przed pierwszym realnym obstawianiem, po zmianie mechanizmu backupow oraz po istotnej migracji bazy.

Procedura:

1. Wybierz punkt odtworzenia z natywnych backupow albo wskaz zatwierdzony zaszyfrowany dump.
2. W DigitalOcean wybierz `Databases -> <production PostgreSQL cluster> -> Actions -> Restore from backup`.
3. Wybierz najnowsza dostepna transakcje albo punkt w czasie z ostatnich 7 dni.
4. Odtworz dane do nowego klastra, nigdy bezposrednio na aktywna produkcje.
5. Nazwij klaster testowy tak, zeby byl jednoznaczny, np. `world-cup-typer-restore-test-YYYYMMDD`.
6. Sprawdz, czy restore nie wymaga zwiekszenia limitu liczby klastrow na koncie/teamie DigitalOcean.
7. Podlacz backend testowy albo stagingowy do odtworzonej bazy przez tymczasowy connection string.
8. Uruchom migracje tylko wtedy, gdy restore ma sprawdzac zgodnosc z nowsza wersja aplikacji.
9. Sprawdz `/health` i `/health/live`.
10. Zaloguj sie jako dedykowane konto testowe, jesli restore odbywa sie w srodowisku, w ktorym wolno uzyc tych danych.
11. Zweryfikuj przykladowo:
    - liczbe uzytkownikow,
    - liczbe meczow,
    - istnienie typow,
    - ranking,
    - ostatnie rozliczone wyniki.
12. Zapisz wynik testu restore w issue lub notatce release: data, punkt odtworzenia, srodowisko, osoba wykonujaca, wynik, znane ograniczenia.
13. Usun tymczasowy klaster po tescie, chyba ze czlowiek zdecydowal inaczej.

## Procedura awaryjnego restore
Restore awaryjny zaczyna sie od decyzji czlowieka, bo moze nadpisac nowsze typy i wyniki.

Minimalna kolejnosc:
1. Zatrzymaj zapisy do aplikacji albo wlacz maintenance mode na poziomie hostingu.
2. Ustal ostatni poprawny punkt w czasie.
3. Zrob snapshot aktualnego stanu, jesli dostawca na to pozwala.
4. W DigitalOcean odtworz backup do nowego klastra.
5. Uruchom backend przeciwko odtworzonej bazie w trybie kontrolnym.
6. Sprawdz health, logowanie, mecze, typy i ranking.
7. Dopiero po akceptacji przelacz produkcyjny connection string na odtworzona baze.
8. Po przelaczeniu odtworz standby/read-only nodes, jesli byly uzywane w oryginalnym klastrze.
9. Zachowaj stara baze do analizy do czasu decyzji o usunieciu.

## Monitoring
Przed startem produkcyjnym trzeba miec widoczny sygnal, ze backupy dzialaja:
- status ostatniego backupu w panelu dostawcy,
- alert albo reczny check przed release, jesli dostawca nie wysyla alertow,
- wpis w checklistach release po kazdym tescie restore.

Jesli zostana wlaczone dodatkowe dumpy, job backupowy musi raportowac sukces i porazke poza repozytorium, np. w panelu hostingu, monitoringu albo recznym runbooku release.

## Checklist przed startem z realnymi typami
- [ ] Czlowiek zatwierdzil dostawce i plan produkcyjnej bazy.
- [ ] Czlowiek zatwierdzil uzycie natywnych backupow/PITR albo alternatywe z szyfrowanymi dumpami.
- [ ] Automatyczne backupy sa wlaczone.
- [ ] Retencja jest znana i zaakceptowana.
- [ ] Lista osob z dostepem do backupow jest znana i minimalna.
- [ ] Wykonano test restore do oddzielnej instancji.
- [ ] Wynik testu restore zostal zapisany w issue lub notatce release.
- [ ] Produkcyjne dane nie trafily do repozytorium, artefaktow CI ani lokalnego katalogu projektu.
