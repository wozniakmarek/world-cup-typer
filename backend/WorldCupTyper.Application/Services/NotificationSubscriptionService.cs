using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Services;

public sealed class NotificationSubscriptionService : INotificationSubscriptionService
{
    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationSubscriptionService(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task SaveSubscriptionAsync(Guid userId, SavePushSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var nowUtc = _dateTimeProvider.UtcNow;
        var deviceId = NormalizeDeviceId(request.DeviceId);
        var subscription = await _dbContext.PushSubscriptions
            .FirstOrDefaultAsync(candidate => candidate.Endpoint == request.Endpoint, cancellationToken);

        if (subscription is null)
        {
            subscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                Endpoint = request.Endpoint,
                CreatedAtUtc = nowUtc,
            };

            await _dbContext.PushSubscriptions.AddAsync(subscription, cancellationToken);
        }

        subscription.UserId = userId;
        subscription.P256dh = request.Keys.P256dh;
        subscription.Auth = request.Keys.Auth;
        subscription.UserAgent = request.UserAgent;
        subscription.DeviceId = deviceId;
        subscription.LastSeenAtUtc = nowUtc;
        subscription.RevokedAtUtc = null;
        subscription.FailureCount = 0;
        subscription.LastFailureAtUtc = null;

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var staleSubscriptions = await _dbContext.PushSubscriptions
                .Where(candidate =>
                    candidate.Id != subscription.Id &&
                    candidate.UserId == userId &&
                    candidate.DeviceId == deviceId &&
                    candidate.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var staleSubscription in staleSubscriptions)
            {
                staleSubscription.RevokedAtUtc = nowUtc;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeSubscriptionAsync(Guid userId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.PushSubscriptions
            .FirstOrDefaultAsync(candidate => candidate.Id == subscriptionId && candidate.UserId == userId, cancellationToken);

        await RevokeSubscriptionAsync(subscription, cancellationToken);
    }

    public async Task RevokeCurrentSubscriptionAsync(Guid userId, RevokePushSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint))
        {
            throw new BusinessRuleException("Subskrypcja push jest niekompletna.");
        }

        var subscription = await _dbContext.PushSubscriptions
            .FirstOrDefaultAsync(candidate => candidate.Endpoint == request.Endpoint && candidate.UserId == userId, cancellationToken);

        await RevokeSubscriptionAsync(subscription, cancellationToken);
    }

    private async Task RevokeSubscriptionAsync(PushSubscription? subscription, CancellationToken cancellationToken)
    {
        if (subscription is null || subscription.RevokedAtUtc is not null)
        {
            return;
        }

        subscription.RevokedAtUtc = _dateTimeProvider.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateRequest(SavePushSubscriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Endpoint)
            || request.Keys is null
            || string.IsNullOrWhiteSpace(request.Keys.P256dh)
            || string.IsNullOrWhiteSpace(request.Keys.Auth))
        {
            throw new BusinessRuleException("Subskrypcja push jest niekompletna.");
        }
    }

    private static string? NormalizeDeviceId(string? deviceId)
    {
        var normalized = deviceId?.Trim();

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
