using WorldCupTyper.Application.Abstractions;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class NoopNotificationService : INotificationService
{
    public Task NotifyPredictionsClosingSoonAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task NotifyRankingUpdatedAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
