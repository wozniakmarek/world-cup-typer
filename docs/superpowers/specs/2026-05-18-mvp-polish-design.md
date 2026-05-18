# MVP Polish Design

**Date:** 2026-05-18

**Status:** Approved for planning

## Goal

Domknac istniejace MVP warstwa UX bez dodawania nowych funkcji produktowych. Zakres obejmuje spojnosc stanow `loading/error/empty/success`, lepsza responsywnosc mobilna oraz czytelniejsze formularze i listy po stronie `Player` i `Admin`.

## Scope

### In scope

- lekkie, wspolne wzorce UI dla stanow zapytan i akcji
- polish ekranow `Dashboard`, `Matches`, `Match details`, `Ranking`, `Profile`
- polish ekranow `Admin dashboard`, `Admin players`, `Admin teams`, `Admin matches`
- poprawa mobilnosci dla list, tabel, kart i formularzy
- ujednolicenie komunikatow sukcesu i bledu

### Out of scope

- nowe endpointy backendu
- zmiany kontraktow API
- nowe funkcje produktowe, w tym wykres progresu, push, import terminarza i automatyczne wyniki
- duzy refactor architektury frontendu

## Recommended Approach

Wybrany kierunek to `Hybrid polish system`.

Najpierw powstaje maly zestaw wspolnych wzorcow interfejsu, a dopiero potem sa one wdrazane ekran po ekranie. Dzieki temu unikamy rozjazdu pomiedzy widokami, ale jednoczesnie poprawiamy realne miejsca, ktore sa juz dzisiaj najbardziej widoczne dla uzytkownika.

To podejscie daje najlepszy balans miedzy szybkoscia prac, spojnoscia i niskim ryzykiem. Nie wymaga zmian backendu i nie rozszerza produktu poza MVP.

## Architecture

Polish pozostaje w granicach obecnej architektury frontendu `React + Vite + TanStack Query + Tailwind`.

Zmiany beda skupione w trzech warstwach:

1. `Shared UI patterns`
   Dodanie lub dopracowanie malych komponentow pomocniczych obslugujacych stany zapytan, alerty oraz responsywne listy/tabele.

2. `Feature pages`
   Zastosowanie tych wzorcow na ekranach gracza i admina bez przebudowy routingu ani klienta API.

3. `Verification`
   Manualna weryfikacja widokow i stanów na roznych viewportach oraz standardowy build produkcyjny frontendu.

## Component Plan

### Shared UI patterns

Planowane sa nastepujace typy elementow:

- `Query state wrapper` lub rownowazny zestaw komponentow
  Odpowiada za spójny `loading`, `error` i `empty` dla ekranow opartych o `useQuery`.

- `Feedback banner / inline alert`
  Ujednolica komunikaty po akcjach typu `zapisano`, `dodano`, `rozliczono`, `nie udalo sie`.

- `Responsive record pattern`
  Wspolny wzorzec dla administracyjnych list i tabel, ktory na duzym ekranie moze zostac tabela, a na malym skladac sie do czytelnych kart lub wierszy blokowych.

### Player pages

#### Dashboard

- wyrazniejsze stany ladowania i pustych sekcji
- lepsze zachowanie siatki kart na mniejszych ekranach
- czytelniejsze komunikaty dla najblizszych meczow i top rankingu

#### Matches

- lepsza czytelnosc filtrow i kart meczowych na telefonie
- spójny stan pusty dla listy po filtrach
- lepsza ekspozycja statusu typu i blokady meczu

#### Match details

- dopracowanie formularza typu i komunikatow sukcesu/bledu
- czytelne rozroznienie: przed kickoffem, po kickoffie, po rozliczeniu
- lepsza prezentacja typow innych graczy po odblokowaniu widocznosci

#### Ranking

- lepsza responsywnosc tabeli rankingu
- czytelniejszy fallback, gdy ranking jest pusty
- lepsze wyróżnienie aktualnego uzytkownika

#### Profile

- bardziej czytelne sekcje progresu i historii typow
- lepszy stan pusty, gdy brakuje danych snapshotow lub typow
- lepszy sklad kart/statystyk na telefonie

### Admin pages

#### Admin dashboard

- czytelniejsze entry pointy do zarzadzania
- wyrazniejsze sekcje liczb i szybkich akcji

#### Admin players

- lepszy podzial pomiedzy tworzeniem i edycja
- czytelniejszy feedback dla resetu hasla i dezaktywacji
- mobilny wzorzec listy graczy zamiast polegania tylko na poziomym scrollu

#### Admin teams

- poprawa czytelnosci formularza i listy druzyn
- czytelniejsze sukcesy i bledy przy zapisie

#### Admin matches

- zmniejszenie wrazenia "sciany formularza"
- lepszy uklad sekcji: formularz meczu, wynik, lista spotkan
- bardziej mobilna prezentacja listy meczow i akcji rozliczeniowych

## Data Flow

Ten etap nie zmienia przeplywu danych pomiedzy frontendem i backendem.

Obowiazuja obecne zasady:

- dane sa pobierane przez `TanStack Query`
- akcje zapisu wykonuja istniejace mutacje
- po sukcesie wykonywana jest invalidacja odpowiednich query keys
- komunikaty dla uzytkownika powstaja wyzej w warstwie widoku, bez zmian w kontrakcie API

W praktyce oznacza to, ze polish ma uporzadkowac prezentacje stanow tych samych danych, a nie zmieniac model interakcji z API.

## Error Handling

Blad ma byc widoczny w sposob bardziej przewidywalny i spójny:

- bledy pobrania danych powinny miec spójny blok z komunikatem i mozliwym kontekstem
- bledy mutacji powinny byc pokazywane blisko miejsca akcji
- sukcesy po akcjach admina i gracza powinny uzywac tego samego tonu i wizualnego wzorca
- stany puste nie powinny byc mylone z bledem

Nie zmieniamy globalnej strategii mapowania bledow z API. Uspójniamy tylko to, jak frontend je pokazuje.

## Responsiveness Rules

Najwazniejsze zasady dla tej rundy:

- formularze wielokolumnowe musza naturalnie skladac sie do jednej kolumny na telefonie
- tabele z duza liczba kolumn nie moga polegac wylacznie na `overflow-x-auto`
- akcje w rekordach powinny pozostac wygodne do tapniecia
- istotne informacje, takie jak status meczu, wynik, typ i punkty, powinny byc widoczne bez dodatkowego scrollowania poziomego

## Testing and Verification

### Required checks

- `npm run build`
- manualne przejscie przez kluczowe widoki gracza
- manualne przejscie przez kluczowe widoki admina
- sprawdzenie malych viewportow dla formularzy, tabel i kart

### Representative UI states to verify

- `loading`
- `error`
- `empty`
- `success`

Wystarczy, aby kazdy z tych stanow byl potwierdzony na przynajmniej reprezentatywnych ekranach, a nie koniecznie na kazdym widoku osobno.

## Risks and Guardrails

### Risks

- zbyt szeroki polish moze zamienic sie w niekontrolowany redesign
- lokalne poprawki na stronach moga wprowadzic niespójne wzorce
- nadmierne ruszanie layoutu moze pogorszyc dzialanie na desktopie

### Guardrails

- bez dodawania nowych funkcji
- bez zmian backendu i kontraktow API
- preferowane male komponenty i celowane poprawki
- kazda zmiana wizualna ma sluzyc czytelnosci albo mobilnosci, nie tylko estetyce

## Success Criteria

Ta runda bedzie uznana za udana, jesli:

- aplikacja bedzie sprawiala wrazenie bardziej dopietej i mniej surowej
- ekrany admina beda wygodniejsze na telefonie
- stany `loading/error/empty/success` beda bardziej spójne
- formularze i listy beda czytelniejsze bez zmian produktowych
- frontend przejdzie build i reczna weryfikacje kluczowych przeplywow

## Implementation Notes

Plan implementacji powinien byc podzielony na:

1. wspolne wzorce UI
2. polish ekranow gracza
3. polish ekranow admina
4. weryfikacje i finalny przeglad

Plan nie powinien obejmowac backendu, migracji ani nowych endpointow.
