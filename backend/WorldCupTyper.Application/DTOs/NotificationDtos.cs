namespace WorldCupTyper.Application.DTOs;

public sealed record NotificationSettingsResponse(
    bool MorningDigestEnabled,
    bool MissingPrediction2hEnabled,
    bool MissingPrediction30mEnabled,
    bool RankingUpdatedEnabled,
    bool HasActiveSubscription);

public sealed record UpdateNotificationSettingsRequest(
    bool MorningDigestEnabled,
    bool MissingPrediction2hEnabled,
    bool MissingPrediction30mEnabled,
    bool RankingUpdatedEnabled);

public sealed record PushSubscriptionKeysRequest(string P256dh, string Auth);

public sealed record SavePushSubscriptionRequest(
    string Endpoint,
    PushSubscriptionKeysRequest Keys,
    string? UserAgent);

public sealed record RevokePushSubscriptionRequest(string Endpoint);
