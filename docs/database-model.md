# Model danych

## Główne encje

### ApplicationUser
- `Id`
- `Email`
- `DisplayName`
- `PasswordHash`
- `Role`
- `IsActive`
- `CreatedAtUtc`
- `LastLoginAtUtc`

### Team
- `Id`
- `Name`
- `ShortName`
- `CountryCode`
- `FlagEmoji`
- `GroupName`

### Match
- `Id`
- `ExternalId`
- `MatchNumber`
- `Phase`
- `GroupName`
- `HomeTeamId`
- `AwayTeamId`
- `HomeSlotRule`
- `AwaySlotRule`
- `KickoffTimeUtc`
- `Venue`
- `Status`
- `HomeScore90`
- `AwayScore90`
- `HomeScoreFinal`
- `AwayScoreFinal`
- `WinnerTeamId`
- `IsSettled`
- `SettledAtUtc`
- `CreatedAtUtc`
- `UpdatedAtUtc`

### Prediction
- `Id`
- `UserId`
- `MatchId`
- `PredictedHomeScore`
- `PredictedAwayScore`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `LockedAtUtc`

### PredictionResult
- `Id`
- `PredictionId`
- `Points`
- `IsExactScore`
- `IsCorrectOutcome`
- `CalculatedAtUtc`

### LeaderboardSnapshot
- `Id`
- `MatchId`
- `UserId`
- `TotalPoints`
- `ExactScoreHits`
- `CorrectOutcomeHits`
- `PredictionsCount`
- `Position`
- `CreatedAtUtc`

## Ograniczenia
- unikalny email użytkownika,
- unikalna nazwa wyświetlana użytkownika,
- unikalny skrót i nazwa drużyny,
- unikalny numer meczu,
- unikalny typ `UserId + MatchId`,
- unikalny snapshot `MatchId + UserId`.
