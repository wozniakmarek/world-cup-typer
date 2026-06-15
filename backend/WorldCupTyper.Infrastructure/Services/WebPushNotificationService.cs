using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class WebPushNotificationService : INotificationService
{
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

        if (subscriptions.Count == 0)
        {
            return;
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var payload = BuildRankingUpdatedPayload(match);
        var subjectKey = $"ranking:{matchId:N}";

        foreach (var subscription in subscriptions)
        {
            var delivery = new NotificationDelivery
            {
                Id = Guid.NewGuid(),
                UserId = subscription.UserId,
                PushSubscriptionId = subscription.Id,
                MatchId = matchId,
                SubjectKey = subjectKey,
                Type = NotificationType.RankingUpdated,
                ScheduledForUtc = nowUtc,
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
            }
            catch (WebPushException exception) when (IsExpiredSubscription(exception.StatusCode))
            {
                subscription.FailureCount++;
                subscription.LastFailureAtUtc = nowUtc;
                subscription.RevokedAtUtc = nowUtc;
                delivery.Status = NotificationDeliveryStatus.Failed;
                delivery.ErrorCode = $"WebPush:{(int)exception.StatusCode}";

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

                _logger.LogWarning(
                    exception,
                    "Failed to send web push ranking notification to subscription {SubscriptionId}.",
                    subscription.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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
