namespace WorldCupTyper.Application.DTOs;

public sealed record LeaderboardEntryDto(
    int Position,
    Guid UserId,
    string DisplayName,
    int TotalPoints,
    int ExactScoreHits,
    int CorrectOutcomeHits,
    int PredictionsCount,
    string? AvatarUrl,
    bool IsCurrentUser);

public sealed record RankingProgressPointDto(
    Guid MatchId,
    int MatchNumber,
    DateTime SnapshotAtUtc,
    int TotalPoints,
    int ExactScoreHits,
    int CorrectOutcomeHits,
    int PredictionsCount,
    int Position);
