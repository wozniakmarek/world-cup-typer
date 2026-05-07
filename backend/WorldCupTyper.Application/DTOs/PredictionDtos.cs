namespace WorldCupTyper.Application.DTOs;

public sealed record SavePredictionRequest(int PredictedHomeScore, int PredictedAwayScore);

public sealed record PredictionSummaryDto(
    Guid Id,
    int PredictedHomeScore,
    int PredictedAwayScore,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? LockedAtUtc,
    int? Points,
    bool? IsExactScore,
    bool? IsCorrectOutcome);

public sealed record MyPredictionDto(
    Guid MatchId,
    string HomeTeamName,
    string AwayTeamName,
    DateTime KickoffTimeUtc,
    PredictionSummaryDto Prediction);

public sealed record MatchPredictionViewDto(
    Guid PredictionId,
    Guid UserId,
    string DisplayName,
    int PredictedHomeScore,
    int PredictedAwayScore,
    int? Points,
    bool? IsExactScore,
    bool? IsCorrectOutcome);

public sealed record MatchPredictionsResponseDto(
    bool CanViewAllPredictions,
    IReadOnlyCollection<MatchPredictionViewDto> Predictions);

public sealed record PredictionScoreDto(int Points, bool IsExactScore, bool IsCorrectOutcome);
