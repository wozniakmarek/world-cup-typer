using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class WebPushNotificationService : INotificationService
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromMinutes(5);
    private const string WarsawWindowsTimeZoneId = "Central European Standard Time";
    private const string WarsawIanaTimeZoneId = "Europe/Warsaw";

    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly WebPushOptions _options;
    private readonly IWebPushSender _sender;
    private readonly ILogger<WebPushNotificationService> _logger;

    public WebPushNotificationService(
        IAppDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IOptions<WebPushOptions> options,
        IWebPushSender sender,
        ILogger<WebPushNotificationService> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _options = NormalizeOptions(options.Value);
        _sender = sender;
        _logger = logger;
    }

    public Task NotifyPredictionsClosingSoonAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<TestNotificationResponse> NotifyDueMatchRemindersAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured(_options))
        {
            _logger.LogWarning("Web push match reminders skipped because VAPID options are not configured.");
            return new TestNotificationResponse(0, 0, 0, 0);
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var aggregate = new TestNotificationResponse(0, 0, 0, 0);

        aggregate = Add(aggregate, await NotifyMorningDigestIfDueAsync(nowUtc, cancellationToken));
        aggregate = Add(aggregate, await NotifyMissingPredictionsIfDueAsync(
            nowUtc,
            TimeSpan.FromHours(2),
            NotificationType.MissingPrediction2h,
            cancellationToken));
        aggregate = Add(aggregate, await NotifyMissingPredictionsIfDueAsync(
            nowUtc,
            TimeSpan.FromMinutes(30),
            NotificationType.MissingPrediction30m,
            cancellationToken));

        return aggregate;
    }

    public async Task NotifyRankingUpdatedAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured(_options))
        {
            _logger.LogWarning("Web push ranking notification skipped because VAPID options are not configured.");
            return;
        }

        var match = await _dbContext.Matches
            .AsNoTracking()
            .Include(candidate => candidate.HomeTeam)
            .Include(candidate => candidate.AwayTeam)
            .FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);

        var subscriptions = await _dbContext.PushSubscriptions
            .Include(subscription => subscription.User)
            .ThenInclude(user => user.NotificationPreference)
            .Where(subscription =>
                subscription.RevokedAtUtc == null &&
                subscription.User.IsActive &&
                (subscription.User.NotificationPreference == null ||
                    subscription.User.NotificationPreference.RankingUpdatedEnabled))
            .ToListAsync(cancellationToken);

        await SendToSubscriptionsAsync(
            subscriptions,
            BuildRankingUpdatedPayload(match),
            NotificationType.RankingUpdated,
            $"ranking:{matchId:N}",
            matchId,
            _dateTimeProvider.UtcNow,
            cancellationToken);
    }

    public async Task<TestNotificationResponse> SendTestNotificationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured(_options))
        {
            _logger.LogWarning("Web push test notification skipped because VAPID options are not configured.");
            return new TestNotificationResponse(0, 0, 0, 0);
        }

        var subscriptions = await _dbContext.PushSubscriptions
            .Include(subscription => subscription.User)
            .Where(subscription =>
                subscription.UserId == userId &&
                subscription.RevokedAtUtc == null &&
                subscription.User.IsActive)
            .ToListAsync(cancellationToken);

        return await SendToSubscriptionsAsync(
            subscriptions,
            BuildTestPayload(),
            NotificationType.Test,
            $"test:{userId:N}",
            null,
            _dateTimeProvider.UtcNow,
            cancellationToken);
    }

    private async Task<TestNotificationResponse> SendToSubscriptionsAsync(
        List<WorldCupTyper.Domain.Entities.PushSubscription> subscriptions,
        string payload,
        NotificationType type,
        string subjectKey,
        Guid? matchId,
        DateTime scheduledForUtc,
        CancellationToken cancellationToken)
    {
        if (subscriptions.Count == 0)
        {
            return new TestNotificationResponse(0, 0, 0, 0);
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var sent = 0;
        var failed = 0;
        var revoked = 0;

        foreach (var subscription in subscriptions)
        {
            var delivery = new NotificationDelivery
            {
                Id = Guid.NewGuid(),
                UserId = subscription.UserId,
                PushSubscriptionId = subscription.Id,
                MatchId = matchId,
                SubjectKey = subjectKey,
                Type = type,
                ScheduledForUtc = scheduledForUtc,
                CreatedAtUtc = nowUtc,
                Status = NotificationDeliveryStatus.Pending,
            };
            await _dbContext.NotificationDeliveries.AddAsync(delivery, cancellationToken);

            try
            {
                await _sender.SendAsync(
                    new WebPushRequest(subscription.Endpoint, subscription.P256dh, subscription.Auth, payload),
                    _options,
                    cancellationToken);

                subscription.FailureCount = 0;
                subscription.LastFailureAtUtc = null;
                delivery.Status = NotificationDeliveryStatus.Sent;
                delivery.SentAtUtc = nowUtc;
                sent++;
            }
            catch (WebPushException exception) when (IsExpiredSubscription(exception.StatusCode))
            {
                subscription.FailureCount++;
                subscription.LastFailureAtUtc = nowUtc;
                subscription.RevokedAtUtc = nowUtc;
                delivery.Status = NotificationDeliveryStatus.Failed;
                delivery.ErrorCode = $"WebPush:{(int)exception.StatusCode}";
                failed++;
                revoked++;

                _logger.LogInformation(
                    exception,
                    "Revoked expired web push subscription {SubscriptionId}.",
                    subscription.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                subscription.FailureCount++;
                subscription.LastFailureAtUtc = nowUtc;
                delivery.Status = NotificationDeliveryStatus.Failed;
                delivery.ErrorCode = exception.GetType().Name;
                failed++;

                _logger.LogWarning(
                    exception,
                    "Failed to send web push {NotificationType} notification to subscription {SubscriptionId}.",
                    type,
                    subscription.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TestNotificationResponse(subscriptions.Count, sent, failed, revoked);
    }

    private async Task<TestNotificationResponse> NotifyMissingPredictionsIfDueAsync(
        DateTime nowUtc,
        TimeSpan leadTime,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var windowStartUtc = nowUtc.Subtract(ReminderWindow);
        var kickoffWindowStartUtc = windowStartUtc.Add(leadTime);
        var kickoffWindowEndUtc = nowUtc.Add(leadTime);
        var matches = await _dbContext.Matches
            .AsNoTracking()
            .Include(match => match.HomeTeam)
            .Include(match => match.AwayTeam)
            .Where(match =>
                !match.IsSettled &&
                match.KickoffTimeUtc > nowUtc &&
                match.KickoffTimeUtc <= kickoffWindowEndUtc &&
                match.KickoffTimeUtc > kickoffWindowStartUtc)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);

        var aggregate = new TestNotificationResponse(0, 0, 0, 0);
        foreach (var match in matches)
        {
            var subjectKey = $"{type}:{match.Id:N}";
            var scheduledForUtc = match.KickoffTimeUtc.Subtract(leadTime);
            var subscriptions = await GetMissingPredictionSubscriptionsAsync(match.Id, type, subjectKey, scheduledForUtc, cancellationToken);

            aggregate = Add(aggregate, await SendToSubscriptionsAsync(
                subscriptions,
                BuildMissingPredictionPayload(match, type),
                type,
                subjectKey,
                match.Id,
                scheduledForUtc,
                cancellationToken));
        }

        return aggregate;
    }

    private async Task<TestNotificationResponse> NotifyMorningDigestIfDueAsync(DateTime nowUtc, CancellationToken cancellationToken)
    {
        var timeZone = GetWarsawTimeZone();
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);
        var localDate = DateOnly.FromDateTime(localNow);
        var scheduledLocal = localDate.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Unspecified);
        var scheduledForUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledLocal, timeZone);

        if (scheduledForUtc > nowUtc || scheduledForUtc <= nowUtc.Subtract(ReminderWindow))
        {
            return new TestNotificationResponse(0, 0, 0, 0);
        }

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), timeZone);
        var todaysMatchIds = await _dbContext.Matches
            .AsNoTracking()
            .Where(match => match.KickoffTimeUtc >= startUtc && match.KickoffTimeUtc < endUtc && match.KickoffTimeUtc > nowUtc)
            .Select(match => match.Id)
            .ToListAsync(cancellationToken);

        if (todaysMatchIds.Count == 0)
        {
            return new TestNotificationResponse(0, 0, 0, 0);
        }

        var predictionPairs = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => todaysMatchIds.Contains(prediction.MatchId))
            .Select(prediction => new { prediction.UserId, prediction.MatchId })
            .ToListAsync(cancellationToken);
        var predictedByUser = predictionPairs
            .GroupBy(prediction => prediction.UserId)
            .ToDictionary(group => group.Key, group => group.Select(prediction => prediction.MatchId).ToHashSet());

        var subjectKey = $"digest:{localDate:yyyy-MM-dd}";
        var subscriptions = await GetActiveSubscriptionsForPreferenceAsync(NotificationType.MorningDigest, cancellationToken);
        var aggregate = new TestNotificationResponse(0, 0, 0, 0);

        foreach (var subscription in subscriptions)
        {
            var predictedMatchIds = predictedByUser.GetValueOrDefault(subscription.UserId) ?? new HashSet<Guid>();
            var missingCount = todaysMatchIds.Count(matchId => !predictedMatchIds.Contains(matchId));
            if (missingCount == 0 || await HasDeliveryAsync(subscription.Id, NotificationType.MorningDigest, subjectKey, scheduledForUtc, cancellationToken))
            {
                continue;
            }

            aggregate = Add(aggregate, await SendToSubscriptionsAsync(
                [subscription],
                BuildMorningDigestPayload(missingCount),
                NotificationType.MorningDigest,
                subjectKey,
                null,
                scheduledForUtc,
                cancellationToken));
        }

        return aggregate;
    }

    private async Task<List<WorldCupTyper.Domain.Entities.PushSubscription>> GetMissingPredictionSubscriptionsAsync(
        Guid matchId,
        NotificationType type,
        string subjectKey,
        DateTime scheduledForUtc,
        CancellationToken cancellationToken)
    {
        var subscriptions = await GetActiveSubscriptionsForPreferenceAsync(type, cancellationToken);
        var userIds = subscriptions.Select(subscription => subscription.UserId).Distinct().ToList();
        var usersWithPredictions = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => prediction.MatchId == matchId && userIds.Contains(prediction.UserId))
            .Select(prediction => prediction.UserId)
            .ToListAsync(cancellationToken);
        var predictedUserIds = usersWithPredictions.ToHashSet();

        var candidates = subscriptions
            .Where(subscription => !predictedUserIds.Contains(subscription.UserId))
            .ToList();

        var result = new List<WorldCupTyper.Domain.Entities.PushSubscription>();
        foreach (var subscription in candidates)
        {
            if (!await HasDeliveryAsync(subscription.Id, type, subjectKey, scheduledForUtc, cancellationToken))
            {
                result.Add(subscription);
            }
        }

        return result;
    }

    private async Task<List<WorldCupTyper.Domain.Entities.PushSubscription>> GetActiveSubscriptionsForPreferenceAsync(
        NotificationType type,
        CancellationToken cancellationToken)
    {
        var subscriptions = await _dbContext.PushSubscriptions
            .Include(subscription => subscription.User)
            .ThenInclude(user => user.NotificationPreference)
            .Where(subscription =>
                subscription.RevokedAtUtc == null &&
                subscription.User.IsActive)
            .ToListAsync(cancellationToken);

        return subscriptions
            .Where(subscription => IsPreferenceEnabled(subscription.User.NotificationPreference, type))
            .ToList();
    }

    private async Task<bool> HasDeliveryAsync(
        Guid subscriptionId,
        NotificationType type,
        string subjectKey,
        DateTime scheduledForUtc,
        CancellationToken cancellationToken)
    {
        return await _dbContext.NotificationDeliveries.AnyAsync(
            delivery =>
                delivery.PushSubscriptionId == subscriptionId &&
                delivery.Type == type &&
                delivery.SubjectKey == subjectKey &&
                delivery.ScheduledForUtc == scheduledForUtc,
            cancellationToken);
    }

    private static string BuildRankingUpdatedPayload(Match? match)
    {
        var body = match is null
            ? "Przeliczono tabelę. Sprawdź aktualny ranking."
            : $"Rozliczono {match.HomeTeam.Name} - {match.AwayTeam.Name}. Sprawdź aktualny ranking.";

        return JsonSerializer.Serialize(new
        {
            title = "Ranking zaktualizowany",
            body,
            url = "/ranking",
            type = "RankingUpdated",
            matchId = match?.Id,
        });
    }

    private static string BuildMissingPredictionPayload(Match match, NotificationType type)
    {
        var title = type == NotificationType.MissingPrediction2h
            ? "Typowanie zamyka sie za 2h"
            : "Ostatnie 30 minut na typ";
        var body = type == NotificationType.MissingPrediction2h
            ? $"{match.HomeTeam.Name} - {match.AwayTeam.Name}: dodaj swoj typ przed rozpoczeciem meczu."
            : $"{match.HomeTeam.Name} - {match.AwayTeam.Name}: brakuje Twojego typu.";

        return JsonSerializer.Serialize(new
        {
            title,
            body,
            url = $"/matches/{match.Id}",
            type = type.ToString(),
            matchId = match.Id,
        });
    }

    private static string BuildMorningDigestPayload(int missingCount)
    {
        return JsonSerializer.Serialize(new
        {
            title = "Dzisiejsze mecze czekaja",
            body = missingCount == 1
                ? "Masz 1 mecz bez typu."
                : $"Masz {missingCount} mecze bez typu.",
            url = "/matches",
            type = NotificationType.MorningDigest.ToString(),
        });
    }

    private static string BuildTestPayload()
    {
        return JsonSerializer.Serialize(new
        {
            title = "Test powiadomień",
            body = "Jeśli to widzisz, push działa na tym urządzeniu.",
            url = "/profile",
            type = "Test",
        });
    }

    private static bool IsConfigured(WebPushOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Subject) &&
            !string.IsNullOrWhiteSpace(options.PublicKey) &&
            !string.IsNullOrWhiteSpace(options.PrivateKey);
    }

    private static bool IsExpiredSubscription(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;
    }

    private static bool IsPreferenceEnabled(NotificationPreference? preference, NotificationType type)
    {
        return type switch
        {
            NotificationType.MorningDigest => preference?.MorningDigestEnabled ?? true,
            NotificationType.MissingPrediction2h => preference?.MissingPrediction2hEnabled ?? true,
            NotificationType.MissingPrediction30m => preference?.MissingPrediction30mEnabled ?? true,
            NotificationType.RankingUpdated => preference?.RankingUpdatedEnabled ?? true,
            NotificationType.Test => true,
            _ => false,
        };
    }

    private static TimeZoneInfo GetWarsawTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WarsawWindowsTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(WarsawIanaTimeZoneId);
        }
    }

    private static TestNotificationResponse Add(TestNotificationResponse left, TestNotificationResponse right)
    {
        return new TestNotificationResponse(
            left.Attempted + right.Attempted,
            left.Sent + right.Sent,
            left.Failed + right.Failed,
            left.Revoked + right.Revoked);
    }

    private static WebPushOptions NormalizeOptions(WebPushOptions options)
    {
        return new WebPushOptions
        {
            Subject = options.Subject?.Trim() ?? string.Empty,
            PublicKey = RemoveWhitespace(options.PublicKey),
            PrivateKey = RemoveWhitespace(options.PrivateKey),
        };
    }

    private static string RemoveWhitespace(string? value)
    {
        return new string((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character)).ToArray());
    }
}
