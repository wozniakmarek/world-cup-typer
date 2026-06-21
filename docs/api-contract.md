# API Contract

Base path for application endpoints is `/api`. JSON enums are serialized as strings.

Unless an endpoint is marked as public, it requires a JWT bearer token. Admin endpoints require the `Admin` role. Invalid model state responses use this shape:

```json
{
  "message": "Dane wejsciowe sa niepoprawne.",
  "errors": {
    "field": ["error"]
  }
}
```

## Health

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/health/live` | Public | Liveness check for the running API process. |
| `GET` | `/health` | Public | Readiness check including PostgreSQL connectivity. |

## Auth

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/auth/login` | Public | Login by email or display name and return JWT plus current user. |
| `POST` | `/api/auth/change-password` | User | Change the current user's password. Required temporary-password users are routed here by middleware. |
| `POST` | `/api/auth/logout` | User | Stateless logout endpoint used by the client flow. |
| `GET` | `/api/auth/me` | User | Return current user profile, role, active state, password-change flag, and avatar. |
| `PUT` | `/api/auth/me/avatar` | User | Save or clear a profile avatar URL or validated image data URL. |

## Players

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/admin/players` | Admin | List players and admins. |
| `POST` | `/api/admin/players` | Admin | Create a player/admin account. |
| `PUT` | `/api/admin/players/{id}` | Admin | Update email, display name, role, and active flag. |
| `POST` | `/api/admin/players/{id}/deactivate` | Admin | Deactivate an account. |
| `POST` | `/api/admin/players/{id}/reset-password` | Admin | Generate or set a temporary password and force password change. |

## Teams

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/teams` | User | List teams used by match forms and match displays. |
| `POST` | `/api/admin/teams` | Admin | Create a team. |
| `PUT` | `/api/admin/teams/{id}` | Admin | Update team identity, country code, flag, and group. |

## Matches

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/matches` | User | List all matches with current user's prediction status and editability. |
| `GET` | `/api/matches/today` | User | List today's matches. |
| `GET` | `/api/matches/upcoming` | User | List upcoming matches. |
| `GET` | `/api/matches/{id}` | User | Get match details, 90-minute score/final score, user's prediction, and visibility flags. |

## Admin Matches

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/admin/matches` | Admin | List matches with admin metadata and prediction counts. |
| `POST` | `/api/admin/matches` | Admin | Create a match. |
| `PUT` | `/api/admin/matches/{id}` | Admin | Update match metadata, teams, phase, kickoff, venue, and status. |
| `PUT` | `/api/admin/matches/{id}/result` | Admin | Save 90-minute score and optional final score/winner. |
| `POST` | `/api/admin/matches/{id}/settle` | Admin | Settle one match and create prediction results plus leaderboard snapshots. |
| `POST` | `/api/admin/matches/recalculate-ranking` | Admin | Rebuild ranking snapshots from stored settled results. |
| `POST` | `/api/admin/matches/sync-football-data` | Admin | Run the football-data.org import on demand. |

## Predictions

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/predictions/my` | User | Return current user's prediction history. |
| `POST` | `/api/matches/{matchId}/prediction` | User | Create a prediction before kickoff. |
| `PUT` | `/api/matches/{matchId}/prediction` | User | Edit a prediction before kickoff. |
| `GET` | `/api/matches/{matchId}/predictions` | User | Return visible predictions for a match. Other players are visible only after kickoff. |

## Ranking

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/ranking/top` | Public | Return public top 5 leaderboard entries. |
| `GET` | `/api/ranking` | User | Return full ranking with current-user marker. |
| `GET` | `/api/ranking/me` | User | Return current user's leaderboard entry. |
| `GET` | `/api/ranking/progress` | User | Return current user's ranking progress points. |
| `GET` | `/api/ranking/progress/all` | User | Return ranking progress series for all players in the ranking chart. |

## Notifications

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| `GET` | `/api/notifications/settings` | User | Return notification preferences and whether the account has an active push subscription. |
| `PUT` | `/api/notifications/settings` | User | Update notification preferences. |
| `GET` | `/api/notifications/vapid-public-key` | User | Return the public VAPID key for browser push subscription. |
| `POST` | `/api/notifications/subscriptions` | User | Register or refresh the current browser/device push subscription. |
| `DELETE` | `/api/notifications/subscriptions/{id}` | User | Revoke a subscription by backend id. |
| `DELETE` | `/api/notifications/subscriptions/current` | User | Revoke the current browser/device subscription by endpoint. |
| `POST` | `/api/notifications/test` | User | Send a test push notification to active subscriptions for the current user. |
