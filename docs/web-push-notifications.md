# Web Push Notifications

This document describes the current Web Push implementation in `world-cup-typer`.

## Current Scope

The application supports real browser push subscriptions and notification delivery for logged-in users:

- account-level notification preferences,
- browser/device subscription registration,
- current-device disable flow,
- VAPID public key endpoint,
- iOS install guidance,
- morning digest,
- 2h missing-prediction reminder,
- 30m missing-prediction reminder,
- ranking update notification,
- test notification endpoint,
- durable delivery records for deduplication and diagnostics,
- expired subscription revocation on `404`/`410` Web Push responses.

## User Flow

1. User opens the profile page.
2. `NotificationSettingsPanel` loads settings from `/api/notifications/settings`.
3. The browser support state is checked:
   - unsupported browser,
   - denied permission,
   - iOS/iPadOS install-required state,
   - existing subscription on the current device.
4. User enables push for the current device.
5. Frontend requests permission, fetches `/api/notifications/vapid-public-key`, registers the service worker, subscribes with `PushManager`, creates a persistent device id, and posts the subscription to `/api/notifications/subscriptions`.
6. User can update account preferences independently from the current-device subscription.
7. User can send a test notification or disable the current device.

## Frontend Files

- `frontend/src/features/profile/NotificationSettingsPanel.tsx`
- `frontend/src/features/profile/webPush.ts`
- `frontend/public/push-sw.js`
- `frontend/vite.config.ts`

Important frontend behavior:

- rejects unsupported browsers,
- handles iPhone/iPad Safari by asking the user to add the PWA to the home screen,
- normalizes VAPID public keys and strips whitespace/BOM,
- stores a stable browser/device id in local storage,
- registers the push service worker at `/sw.js`,
- sends `endpoint`, `p256dh`, `auth`, `userAgent`, and `deviceId` to the backend.

## Backend Files

- `NotificationsController`
- `NotificationPreferenceService`
- `NotificationSubscriptionService`
- `WebPushNotificationService`
- `NotificationReminderWorker`
- `WebPushSender`
- `WebPushOptionsValidator`
- `PushSubscription`
- `NotificationPreference`
- `NotificationDelivery`

## API

See [API contract](api-contract.md) for full endpoint listing.

Notification endpoints:

- `GET /api/notifications/settings`
- `PUT /api/notifications/settings`
- `GET /api/notifications/vapid-public-key`
- `POST /api/notifications/subscriptions`
- `DELETE /api/notifications/subscriptions/{id}`
- `DELETE /api/notifications/subscriptions/current`
- `POST /api/notifications/test`

## Data Model

See [Data model](database-model.md) for field-level detail.

The notification model is split into:

- `PushSubscription` - one active/revoked browser/device subscription.
- `NotificationPreference` - account-level toggles.
- `NotificationDelivery` - one attempted delivery record used for deduplication and diagnostics.

Delivery uniqueness is enforced by:

```text
UserId + PushSubscriptionId + Type + SubjectKey + ScheduledForUtc
```

## Notification Types

### Morning Digest

Runs around 07:00 Europe/Warsaw and sends a daily summary only when the user still has missing predictions for upcoming matches that day.

Subject key:

```text
digest:yyyy-MM-dd
```

### Missing Prediction 2h

Runs in the reminder worker window for matches starting in about two hours. Sends only to active users without a prediction for the match and with `MissingPrediction2hEnabled=true`.

Subject key:

```text
MissingPrediction2h:{matchId}
```

### Missing Prediction 30m

Runs in the reminder worker window for matches starting in about 30 minutes. Sends only to active users without a prediction for the match and with `MissingPrediction30mEnabled=true`.

Subject key:

```text
MissingPrediction30m:{matchId}
```

### Ranking Updated

Sent after match settlement when ranking has been recalculated and the user has `RankingUpdatedEnabled=true`.

Subject key:

```text
ranking:{matchId}
```

### Test

Manual user-triggered test from the profile page.

Subject key:

```text
test:{userId}
```

## Worker Behavior

`NotificationReminderWorker` runs `INotificationService.NotifyDueMatchRemindersAsync()` on an interval.

Configuration:

- `NotificationReminders:Enabled`
- `NotificationReminders:IntervalMinutes`

The worker exits early when disabled. Failures are logged and do not crash the host.

## Production Configuration

Production requires:

- `WebPush__Subject`
- `WebPush__PublicKey`
- `WebPush__PrivateKey`

The DigitalOcean deployment workflow maps these from:

- `WEB_PUSH_PUBLIC_KEY`
- `WEB_PUSH_PRIVATE_KEY`
- `WEB_PUSH_SUBJECT`

The private key must remain secret. The public key is intentionally exposed through the API so browsers can subscribe.

## Safety Notes

- Do not log endpoints, auth secrets, or private VAPID keys.
- Expired subscriptions are marked revoked after Web Push `404` or `410`.
- Account preferences and browser/device subscription state are separate.
- Production testing that changes real user notification data requires a human decision.
