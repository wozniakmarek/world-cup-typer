using WorldCupTyper.Application.Abstractions;

using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class NoopNotificationService : INotificationService
{
    public Task NotifyPredictionsClosingSoonAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TestNotificationResponse> NotifyDueMatchRemindersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestNotificationResponse(0, 0, 0, 0));
    }

    public Task NotifyRankingUpdatedAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TestNotificationResponse> SendTestNotificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestNotificationResponse(0, 0, 0, 0));
    }
}
