using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface INotificationPreferenceService
{
    Task<NotificationSettingsResponse> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationSettingsResponse> UpdateSettingsAsync(Guid userId, UpdateNotificationSettingsRequest request, CancellationToken cancellationToken = default);
}
