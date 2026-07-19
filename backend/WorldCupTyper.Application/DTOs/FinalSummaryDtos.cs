namespace WorldCupTyper.Application.DTOs;

public sealed record FinalSummaryStatsDto(
    int SettledMatchesCount,
    int ActivePlayersCount,
    Guid? FinalLeaderUserId,
    string? FinalLeaderDisplayName);

public sealed record FinalSummaryAvailabilityDto(
    bool IsReady,
    string Reason,
    int SettledMatchesCount,
    int RequiredSettledMatchesCount,
    int TotalMatchesCount,
    string? FinalMatchLabel);

public sealed record FinalSummaryResponseDto(
    FinalSummaryStatsDto Stats,
    IReadOnlyCollection<FinalRankingPositionSeriesDto> PositionSeries,
    IReadOnlyCollection<FinalRankingEntryDto> FinalTop,
    IReadOnlyCollection<FinalSummaryFactDto> GlobalFacts);

public sealed record FinalRankingPositionSeriesDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    int FinalPosition,
    int FinalPoints,
    bool IsCurrentUser,
    IReadOnlyCollection<FinalRankingPositionPointDto> Points);

public sealed record FinalRankingPositionPointDto(
    Guid MatchId,
    int MatchNumber,
    string MatchLabel,
    DateTime SnapshotAtUtc,
    int Position,
    int TotalPoints);

public sealed record FinalRankingEntryDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    int FinalPosition,
    int TotalPoints,
    int ExactScoreHits,
    int CorrectOutcomeHits,
    int PredictionsCount,
    bool IsCurrentUser);

public sealed record FinalSummaryFactDto(
    string Id,
    string Label,
    string Title,
    string Description,
    IReadOnlyCollection<Guid> RelatedUserIds,
    IReadOnlyCollection<Guid> RelatedMatchIds);

public sealed record PersonalFinalSummaryResponseDto(
    Guid UserId,
    string DisplayName,
    string? AvatarUrl,
    int FinalPosition,
    int TotalPoints,
    int ExactScoreHits,
    int CorrectOutcomeHits,
    int PredictionsCount,
    IReadOnlyCollection<FinalSummaryFactDto> PersonalFacts,
    IReadOnlyCollection<Guid> HighlightedMatchIds);
