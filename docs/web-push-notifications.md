# Web Push Notifications - plan techniczny

## Cel
Pierwszy etap web push notifications ma przygotowac fundament pod przypomnienia bez wprowadzania ciezkiej orkiestracji. MVP powinno zapisac zgody i subskrypcje, pozwolic backendowi wybrac odbiorcow oraz zostawic proste, testowalne punkty rozszerzenia dla wysylki.

Zakres tego etapu to design i skeleton. Pelna wysylka push, monitoring kolejek i zaawansowana personalizacja moga powstac pozniej.

## Obecne punkty zaczepienia
- `Match.KickoffTimeUtc` jest zrodlem prawdy dla zamkniecia typowania.
- `PredictionService` blokuje tworzenie i edycje typow po kickoffie przez `Match.CanAcceptPredictions(nowUtc)`.
- `MatchSettlementService` rozlicza typy, zapisuje `LeaderboardSnapshot` i ustawia `Match.Status = Settled`.
- `INotificationService` oraz `NoopNotificationService` juz istnieja jako bezpieczny seam pod przyszle powiadomienia.
- Frontend jest PWA-ready i ma `PwaInstallPrompt`, ale nie ma jeszcze service workera push ani flow zgody na powiadomienia.

## Zasady MVP
- Zgoda na powiadomienia jest opt-in i pokazywana po zalogowaniu, nie podczas logowania.
- Backend przechowuje tylko techniczne dane Web Push oraz preferencje uzytkownika.
- Endpoint subskrypcji wymaga JWT i zawsze przypisuje subskrypcje do `User.GetUserId()`.
- Wysylka musi byc idempotentna per `UserId + NotificationType + SubjectKey + ScheduledForUtc`.
- Nie wysylamy pushy do nieaktywnych uzytkownikow.
- Production data changes zwiazane z testami push wymagaja decyzji czlowieka.

## Model danych

### PushSubscription
Encja reprezentuje jedna przegladarke albo urzadzenie uzytkownika.

Pola:
- `Id`
- `UserId`
- `Endpoint`
- `P256dh`
- `Auth`
- `UserAgent`
- `CreatedAtUtc`
- `LastSeenAtUtc`
- `RevokedAtUtc`
- `FailureCount`
- `LastFailureAtUtc`

Ograniczenia:
- unikalny `Endpoint`,
- indeks `UserId`,
- indeks `RevokedAtUtc`,
- endpoint i klucze nie sa sekretami aplikacji, ale nadal nie powinny trafiac do logow.

### NotificationPreference
Preferencje mozna zaczac jako jeden rekord na uzytkownika.

Pola:
- `UserId`
- `MorningDigestEnabled`
- `MissingPrediction2hEnabled`
- `MissingPrediction30mEnabled`
- `RankingUpdatedEnabled`
- `QuietHoursStartLocal`
- `QuietHoursEndLocal`
- `UpdatedAtUtc`

Domyslnie wszystkie cztery typy moga byc wlaczone po zgodzie na push. Quiet hours moga zostac niewykorzystane w pierwszym wdrozeniu, ale pole pozwala uniknac migracji modelu przy pierwszej personalizacji.

### NotificationDelivery
Lekki dziennik deduplikacji i diagnostyki.

Pola:
- `Id`
- `UserId`
- `PushSubscriptionId`
- `MatchId`
- `SubjectKey`
- `Type`
- `ScheduledForUtc`
- `SentAtUtc`
- `Status`
- `ErrorCode`
- `CreatedAtUtc`

Ograniczenia:
- unikalny klucz `UserId + PushSubscriptionId + Type + SubjectKey + ScheduledForUtc`,
- `SubjectKey` jest stabilnym kluczem deduplikacji, np. `match:{matchId}` albo `digest:2026-06-13`,
- `MatchId` jest opcjonalnym kontekstem nawigacji i raportowania,
- statusy: `Pending`, `Sent`, `Skipped`, `Failed`.

## API

### `GET /api/notifications/settings`
Zwraca obecne preferencje oraz stan po stronie przegladarki znany backendowi.

Response:
```json
{
  "morningDigestEnabled": true,
  "missingPrediction2hEnabled": true,
  "missingPrediction30mEnabled": true,
  "rankingUpdatedEnabled": true,
  "hasActiveSubscription": true
}
```

### `PUT /api/notifications/settings`
Aktualizuje preferencje zalogowanego uzytkownika.

Request:
```json
{
  "morningDigestEnabled": true,
  "missingPrediction2hEnabled": true,
  "missingPrediction30mEnabled": true,
  "rankingUpdatedEnabled": true
}
```

### `POST /api/notifications/subscriptions`
Rejestruje albo odswieza subskrypcje Web Push dla aktualnego uzytkownika. Endpoint powinien byc idempotentny po `endpoint`.

Request:
```json
{
  "endpoint": "https://push.example/browser-token",
  "keys": {
    "p256dh": "base64-url-key",
    "auth": "base64-url-auth"
  },
  "userAgent": "Mozilla/5.0 (Test Browser)"
}
```

Response: `204 No Content`.

### `DELETE /api/notifications/subscriptions/{id}`
Oznacza subskrypcje jako wycofana (`RevokedAtUtc`). Frontend moze wywolac to przy wylaczeniu powiadomien w profilu. Gdy przegladarka nie zna `id`, mozna dodac wariant `DELETE /api/notifications/subscriptions/current` z endpointem w body.

### `DELETE /api/notifications/subscriptions/current`
Oznacza aktualna subskrypcje przegladarki jako wycofana po `endpoint` przekazanym w body. Ten wariant jest uzywany przez frontend, bo Web Push API zna endpoint, ale nie zna backendowego `Id`.

### Konfiguracja VAPID
Backend potrzebuje:
- `WebPush:Subject`
- `WebPush:PublicKey`
- `WebPush:PrivateKey`

Klucz publiczny moze byc zwracany frontendowi przez `GET /api/notifications/vapid-public-key`, a prywatny klucz pozostaje w konfiguracji hostingu.

## Flow zgody po stronie frontend
1. Po zalogowaniu frontend sprawdza `Notification` i `serviceWorker` w przegladarce.
2. UI pokazuje spokojny prompt w profilu albo dashboardzie, bez blokowania glownego workflow.
3. Po kliknieciu uzytkownika frontend wywoluje `Notification.requestPermission()`.
4. Dla `granted` frontend rejestruje service worker, pobiera VAPID public key i tworzy `PushSubscription`.
5. Subskrypcja trafia do `POST /api/notifications/subscriptions`.
6. Uzytkownik moze zmienic preferencje przez `PUT /api/notifications/settings`.
7. Dla `denied` frontend nie ponawia natretnie prompta. Pokazuje tylko pasywny stan w profilu.

Service worker powinien obslugiwac `push` i `notificationclick`. Klikniecie w powiadomienie prowadzi do:
- `/matches/{matchId}` dla przypomnien meczowych,
- `/ranking` dla aktualizacji rankingu.

## Reguly powiadomien

### Rano przypomnienie o meczach
Cel: pokazac liste dzisiejszych meczow i liczbe brakujacych typow.

Regula:
- job raz dziennie, np. 07:00 Europe/Warsaw,
- wybiera mecze z kickoffem w lokalnym dniu,
- dla kazdego aktywnego uzytkownika liczy mecze bez typu,
- wysyla tylko gdy istnieje przynajmniej jeden mecz bez typu albo uzytkownik ma wlaczone ogolne przypomnienie o terminarzu.

Payload:
```json
{
  "title": "Dzisiejsze mecze czekaja",
  "body": "Masz 2 mecze bez typu.",
  "url": "/matches",
  "type": "MorningDigest"
}
```

### 2h przed meczem bez typu
Cel: pierwsze praktyczne przypomnienie dla konkretnego meczu.

Regula:
- job cykliczny co 5 minut,
- okno: `KickoffTimeUtc - 2h <= nowUtc < KickoffTimeUtc - 1h55m`,
- odbiorcy: aktywni uzytkownicy bez `Prediction` dla tego meczu,
- wysylka tylko dla preferencji `MissingPrediction2hEnabled`.

### 30 min przed meczem bez typu
Cel: ostatnie przypomnienie przed zamknieciem typowania.

Regula:
- job cykliczny co 5 minut,
- okno: `KickoffTimeUtc - 30m <= nowUtc < KickoffTimeUtc - 25m`,
- odbiorcy: aktywni uzytkownicy bez `Prediction` dla tego meczu,
- wysylka tylko dla preferencji `MissingPrediction30mEnabled`.

### Po rozliczeniu meczu i aktualizacji rankingu
Cel: poinformowac, ze punkty i ranking sa gotowe.

Regula:
- najlepszy trigger to koniec `MatchSettlementService.SettleMatchInternalAsync`, po utworzeniu `LeaderboardSnapshot`,
- dla automatycznego syncu i recznego rozliczenia trigger jest ten sam,
- odbiorcy: aktywni uzytkownicy z preferencja `RankingUpdatedEnabled`,
- payload prowadzi do `/ranking` albo `/matches/{matchId}`.

W pierwszym szkielecie mozna zostawic wywolanie na interfejsie:
```csharp
await _notificationService.NotifyRankingUpdatedAsync(match.Id, cancellationToken);
```

## Backendowy podzial odpowiedzialnosci
- `Domain`: encje `PushSubscription`, `NotificationPreference`, `NotificationDelivery` oraz enum `NotificationType`.
- `Application`: DTO, `INotificationSubscriptionService`, `INotificationPreferenceService`, rozszerzony `INotificationService`.
- `Infrastructure`: EF konfiguracje, migracja, implementacja `WebPushNotificationService`, hosted service do planowanych powiadomien.
- `Api`: `NotificationsController` z endpointami uzytkownika.

W MVP `NoopNotificationService` moze pozostac domyslny, dopoki `WebPush:Enabled` nie zostanie wlaczone. Dzieki temu migracje i endpointy subskrypcji moga wejsc bez ryzyka przypadkowej wysylki.

## Backlog implementacyjny

### Etap 1: kontrakty i baza
- Dodac encje domenowe oraz enumy powiadomien.
- Rozszerzyc `IAppDbContext` i `WorldCupTyperDbContext`.
- Dodac konfiguracje EF Core i migracje.
- Dodac testy konfiguracji unikalnosci dla `PushSubscription.Endpoint` i deduplikacji `NotificationDelivery`.

### Etap 2: API subskrypcji i preferencji
- Dodac DTO request/response.
- Dodac serwis zapisujacy subskrypcje idempotentnie po endpoint.
- Dodac serwis preferencji z domyslnymi wartosciami.
- Dodac `NotificationsController`.
- Dodac testy autoryzacji i reguly, ze uzytkownik nie moze zarzadzac cudza subskrypcja.
- Zaktualizowac `docs/api-contract.md` i `docs/database-model.md`.

### Etap 3: frontend consent skeleton
- Dodac `notificationsApi` w `frontend/src/api/services.ts`.
- Dodac typy request/response w `frontend/src/api/types.ts`.
- Dodac service worker push w publicznym katalogu frontendu.
- Dodac komponent ustawien w profilu albo dashboardzie.
- Dodac obsluge braku wsparcia przegladarki, `denied` i `granted`.

### Etap 4: planowanie bez realnej wysylki
- Dodac query wybierajace odbiorcow dla morning digest, 2h, 30m i ranking update.
- Zapisywac `NotificationDelivery` jako `Skipped` albo `Pending` przy wylaczonym `WebPush:Enabled`.
- Dodac testy dla okien czasowych i braku typu.
- Podlaczyc `NotifyRankingUpdatedAsync` po settlement.

### Etap 5: realna wysylka Web Push
- Dodac biblioteke Web Push po stronie .NET.
- Dodac konfiguracje VAPID i endpoint public key.
- Implementowac wysylke z obsluga 404/410 przez `RevokedAtUtc`.
- Logowac bledy bez endpointow i kluczy.
- Dodac staging-first smoke test: rejestracja subskrypcji testowej i weryfikacja, ze backend probuje wyslac payload.

## Weryfikacja
- Unit testy serwisow subskrypcji, preferencji, selection query i deduplikacji delivery.
- Testy kontrolera dla wymaganej autoryzacji.
- Testy EF/migracji dla indeksow.
- Frontend: test komponentu albo Playwright smoke dla widocznego stanu zgody, bez wymagania prawdziwego systemowego prompta.
- Staging-first smoke przed wlaczeniem `WebPush:Enabled` w produkcji.

## Decyzje odlozone
- Czy morning digest ma miec osobna godzine per uzytkownik.
- Czy preferencje maja byc per turniej, czy globalne dla konta.
- Czy potrzebna jest kolejka zewnetrzna. Dla MVP hosted service i `NotificationDelivery` wystarcza.
- Czy admin potrzebuje endpointu testowego do wyslania powiadomienia do siebie.
