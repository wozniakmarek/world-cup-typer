using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Services;

public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationPreferenceService(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<NotificationSettingsResponse> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await ToResponseAsync(userId, preference, cancellationToken);
    }

    public async Task<NotificationSettingsResponse> UpdateSettingsAsync(Guid userId, UpdateNotificationSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var preference = await GetOrCreatePreferenceAsync(userId, cancellationToken);

        preference.MorningDigestEnabled = request.MorningDigestEnabled;
        preference.MissingPrediction2hEnabled = request.MissingPrediction2hEnabled;
        preference.MissingPrediction30mEnabled = request.MissingPrediction30mEnabled;
        preference.RankingUpdatedEnabled = request.RankingUpdatedEnabled;
        preference.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(userId, preference, cancellationToken);
    }

    private async Task<NotificationPreference> GetOrCreatePreferenceAsync(Guid userId, CancellationToken cancellationToken)
    {
        var preference = await _dbContext.NotificationPreferences
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (preference is not null)
        {
            return preference;
        }

        preference = new NotificationPreference
        {
            UserId = userId,
            UpdatedAtUtc = _dateTimeProvider.UtcNow,
        };

        await _dbContext.NotificationPreferences.AddAsync(preference, cancellationToken);
        return preference;
    }

    private async Task<NotificationSettingsResponse> ToResponseAsync(Guid userId, NotificationPreference preference, CancellationToken cancellationToken)
    {
        var hasActiveSubscription = await _dbContext.PushSubscriptions.AnyAsync(
            subscription => subscription.UserId == userId && subscription.RevokedAtUtc == null,
            cancellationToken);

        return new NotificationSettingsResponse(
            preference.MorningDigestEnabled,
            preference.MissingPrediction2hEnabled,
            preference.MissingPrediction30mEnabled,
            preference.RankingUpdatedEnabled,
            hasActiveSubscription);
    }
}
