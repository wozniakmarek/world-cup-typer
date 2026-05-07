using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface IPredictionService
{
    Task<IReadOnlyCollection<MyPredictionDto>> GetMyPredictionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PredictionSummaryDto> CreatePredictionAsync(Guid userId, Guid matchId, SavePredictionRequest request, CancellationToken cancellationToken = default);
    Task<PredictionSummaryDto> UpdatePredictionAsync(Guid userId, Guid matchId, SavePredictionRequest request, CancellationToken cancellationToken = default);
    Task<MatchPredictionsResponseDto> GetPredictionsForMatchAsync(Guid requesterUserId, bool isAdmin, Guid matchId, CancellationToken cancellationToken = default);
}
