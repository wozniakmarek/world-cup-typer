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
    string MatchLabel,
    DateTime SnapshotAtUtc,
    int TotalPoints,
    int ExactScoreHits,
    int CorrectOutcomeHits,
    int PredictionsCount,
    int Position);

public sealed record RankingProgressSeriesDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    bool IsCurrentUser,
    IReadOnlyCollection<RankingProgressPointDto> Points);
