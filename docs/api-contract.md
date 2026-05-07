# API Contract MVP

## Auth
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`

## Admin Players
- `GET /api/admin/players`
- `POST /api/admin/players`
- `PUT /api/admin/players/{id}`
- `POST /api/admin/players/{id}/deactivate`
- `POST /api/admin/players/{id}/reset-password`

## Teams
- `GET /api/teams`
- `POST /api/admin/teams`
- `PUT /api/admin/teams/{id}`

## Matches
- `GET /api/matches`
- `GET /api/matches/today`
- `GET /api/matches/upcoming`
- `GET /api/matches/{id}`

## Admin Matches
- `GET /api/admin/matches`
- `POST /api/admin/matches`
- `PUT /api/admin/matches/{id}`
- `PUT /api/admin/matches/{id}/result`
- `POST /api/admin/matches/{id}/settle`
- `POST /api/admin/matches/recalculate-ranking`

## Predictions
- `GET /api/predictions/my`
- `POST /api/matches/{matchId}/prediction`
- `PUT /api/matches/{matchId}/prediction`
- `GET /api/matches/{matchId}/predictions`

## Ranking
- `GET /api/ranking`
- `GET /api/ranking/top`
- `GET /api/ranking/me`
- `GET /api/ranking/progress`
