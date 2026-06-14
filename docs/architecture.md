# Architektura

## Monorepo
- `frontend/` — React + Vite + TypeScript + Tailwind + React Router + TanStack Query.
- `backend/` — solution .NET 8 z warstwami `Api`, `Application`, `Domain`, `Infrastructure`, `Tests`.
- `docs/` — opis produktu, architektury, modelu danych i uruchomienia.

## Backend
- `Domain` przechowuje encje i enumy.
- `Application` zawiera DTO, interfejsy oraz serwisy biznesowe.
- `Infrastructure` obsługuje EF Core, DbContext, JWT, hashowanie haseł, seed oraz stuby przyszłych integracji.
- `Api` udostępnia REST API, middleware błędów, konfigurację auth/CORS i start aplikacji.

## Frontend
- `app/` — shell, query client, routing, helpery formatowania.
- `api/` — klient HTTP i kontrakty typów.
- `components/` — wspólne elementy UI.
- `features/` — ekrany biznesowe: auth, dashboard, matches, ranking, profile, admin, pwa.

## Kierunek rozwoju
- `LeaderboardSnapshot` umożliwia późniejszy wykres progresu.
- Interfejsy `INotificationService`, `IScheduleImportService` i `IKnockoutResolverService` zostawiają miejsce na następne etapy bez przebudowy obecnych usług.
- `NotificationsController` i encje `PushSubscription`, `NotificationPreference`, `NotificationDelivery` tworzą fundament pod web push bez realnej wysyłki w MVP.
- Model `Match` ma pola pod fazę pucharową, wynik po 90 minutach i wynik końcowy.
