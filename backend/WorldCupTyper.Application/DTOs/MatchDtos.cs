using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Application.DTOs;

public sealed record MatchSummaryDto(
    Guid Id,
    int MatchNumber,
    MatchPhase Phase,
    string? GroupName,
    DateTime KickoffTimeUtc,
    string? Venue,
    MatchStatus Status,
    bool IsSettled,
    int? HomeScore90,
    int? AwayScore90,
    TeamDto HomeTeam,
    TeamDto AwayTeam,
    PredictionSummaryDto? MyPrediction,
    int? MyPoints,
    bool CanEditPrediction);

public sealed record MatchDetailsDto(
    Guid Id,
    int MatchNumber,
    MatchPhase Phase,
    string? GroupName,
    DateTime KickoffTimeUtc,
    string? Venue,
    MatchStatus Status,
    bool IsSettled,
    int? HomeScore90,
    int? AwayScore90,
    int? HomeScoreFinal,
    int? AwayScoreFinal,
    TeamDto HomeTeam,
    TeamDto AwayTeam,
    PredictionSummaryDto? MyPrediction,
    int? MyPoints,
    bool CanEditPrediction,
    bool CanViewPredictions);

public sealed record AdminMatchDto(
    Guid Id,
    int MatchNumber,
    MatchPhase Phase,
    string? GroupName,
    DateTime KickoffTimeUtc,
    string? Venue,
    MatchStatus Status,
    bool IsSettled,
    int PredictionsCount,
    int? HomeScore90,
    int? AwayScore90,
    TeamDto HomeTeam,
    TeamDto AwayTeam);

public sealed record UpsertMatchRequest(
    string? ExternalId,
    int MatchNumber,
    MatchPhase Phase,
    string? GroupName,
    Guid HomeTeamId,
    Guid AwayTeamId,
    string? HomeSlotRule,
    string? AwaySlotRule,
    DateTime KickoffTimeUtc,
    string? Venue,
    MatchStatus Status = MatchStatus.Scheduled);

public sealed record SetMatchResultRequest(
    int HomeScore90,
    int AwayScore90,
    int? HomeScoreFinal,
    int? AwayScoreFinal,
    Guid? WinnerTeamId);
