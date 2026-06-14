using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface INotificationSubscriptionService
{
    Task SaveSubscriptionAsync(Guid userId, SavePushSubscriptionRequest request, CancellationToken cancellationToken = default);
    Task RevokeSubscriptionAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task RevokeCurrentSubscriptionAsync(Guid userId, RevokePushSubscriptionRequest request, CancellationToken cancellationToken = default);
}
