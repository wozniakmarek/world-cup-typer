using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Abstractions;

public interface INotificationService
{
    Task NotifyPredictionsClosingSoonAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task NotifyRankingUpdatedAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<TestNotificationResponse> SendTestNotificationAsync(Guid userId, CancellationToken cancellationToken = default);
}
