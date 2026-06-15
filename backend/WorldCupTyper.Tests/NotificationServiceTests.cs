using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Options;
using WorldCupTyper.Infrastructure.Services;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task GetSettingsAsync_WhenMissingPreference_ShouldCreateDefaultEnabledSettings()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 13, 8, 0, 0, DateTimeKind.Utc) };
        var service = new NotificationPreferenceService(dbContext, dateTimeProvider);

        var settings = await service.GetSettingsAsync(user.Id);

        settings.Should().BeEquivalentTo(new NotificationSettingsResponse(
            MorningDigestEnabled: true,
            MissingPrediction2hEnabled: true,
            MissingPrediction30mEnabled: true,
            RankingUpdatedEnabled: true,
            HasActiveSubscription: false));
        dbContext.NotificationPreferences.Single().UpdatedAtUtc.Should().Be(dateTimeProvider.UtcNow);
    }

    [Fact]
    public async Task SaveSubscriptionAsync_WithExistingEndpoint_ShouldUpdateSubscriptionForCurrentUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var originalUser = CreateUser("old@test.local", "Old User");
        var currentUser = CreateUser("current@test.local", "Current User");
        dbContext.Users.AddRange(originalUser, currentUser);

        var existing = new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = originalUser.Id,
            Endpoint = "https://push.example/token",
            P256dh = "old-key",
            Auth = "old-auth",
            UserAgent = "Old Browser",
            CreatedAtUtc = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc),
            LastSeenAtUtc = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc),
            RevokedAtUtc = new DateTime(2026, 6, 11, 8, 0, 0, DateTimeKind.Utc),
            FailureCount = 2,
            LastFailureAtUtc = new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
        };
        dbContext.PushSubscriptions.Add(existing);
        await dbContext.SaveChangesAsync();

        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 13, 9, 30, 0, DateTimeKind.Utc) };
        var service = new NotificationSubscriptionService(dbContext, dateTimeProvider);

        await service.SaveSubscriptionAsync(
            currentUser.Id,
            new SavePushSubscriptionRequest(
                "https://push.example/token",
                new PushSubscriptionKeysRequest("new-key", "new-auth"),
                "New Browser"));

        dbContext.PushSubscriptions.Should().ContainSingle();
        dbContext.PushSubscriptions.Single().Should().BeEquivalentTo(
            new
            {
                UserId = currentUser.Id,
                Endpoint = "https://push.example/token",
                P256dh = "new-key",
                Auth = "new-auth",
                UserAgent = "New Browser",
                CreatedAtUtc = existing.CreatedAtUtc,
                LastSeenAtUtc = dateTimeProvider.UtcNow,
                RevokedAtUtc = (DateTime?)null,
                FailureCount = 0,
                LastFailureAtUtc = (DateTime?)null,
            });
    }

    [Fact]
    public async Task SaveSubscriptionAsync_WithMissingKeys_ShouldThrowBusinessRuleException()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = new NotificationSubscriptionService(dbContext, new TestDateTimeProvider());

        var act = () => service.SaveSubscriptionAsync(
            Guid.NewGuid(),
            new SavePushSubscriptionRequest("https://push.example/token", null!, "Test Browser"));

        await act.Should()
            .ThrowAsync<BusinessRuleException>()
            .WithMessage("Subskrypcja push jest niekompletna.");
    }

    [Fact]
    public async Task RevokeCurrentSubscriptionAsync_ShouldRevokeOnlyCurrentUsersEndpoint()
    {
        using var dbContext = TestDbContextFactory.Create();
        var currentUser = CreateUser("current@test.local", "Current User");
        var otherUser = CreateUser("other@test.local", "Other User");
        dbContext.Users.AddRange(currentUser, otherUser);
        dbContext.PushSubscriptions.AddRange(
            CreateSubscription(currentUser.Id, "https://push.example/current"),
            CreateSubscription(otherUser.Id, "https://push.example/current"));
        await dbContext.SaveChangesAsync();

        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc) };
        var service = new NotificationSubscriptionService(dbContext, dateTimeProvider);

        await service.RevokeCurrentSubscriptionAsync(
            currentUser.Id,
            new RevokePushSubscriptionRequest("https://push.example/current"));

        dbContext.PushSubscriptions.Single(subscription => subscription.UserId == currentUser.Id).RevokedAtUtc.Should().Be(dateTimeProvider.UtcNow);
        dbContext.PushSubscriptions.Single(subscription => subscription.UserId == otherUser.Id).RevokedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task NotifyRankingUpdatedAsync_WithEnabledActiveSubscription_ShouldSendPushAndRecordDelivery()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 15, 20, 45, 0, DateTimeKind.Utc) };
        var homeTeam = CreateTeam("Poland", "POL");
        var awayTeam = CreateTeam("Germany", "GER");
        var match = CreateMatch(homeTeam.Id, awayTeam.Id);
        var user = CreateUser();
        var subscription = CreateSubscription(user.Id, "https://push.example/ranking");

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        dbContext.Users.Add(user);
        dbContext.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = user.Id,
            RankingUpdatedEnabled = true,
            UpdatedAtUtc = dateTimeProvider.UtcNow,
        });
        dbContext.PushSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var sender = new FakeWebPushSender();
        var service = new WebPushNotificationService(
            dbContext,
            dateTimeProvider,
            Options.Create(new WebPushOptions
            {
                PublicKey = "public-key",
                PrivateKey = "private-key",
                Subject = "mailto:test@example.com",
            }),
            sender,
            NullLogger<WebPushNotificationService>.Instance);

        await service.NotifyRankingUpdatedAsync(match.Id);

        sender.Requests.Should().ContainSingle();
        var request = sender.Requests.Single();
        request.Endpoint.Should().Be(subscription.Endpoint);
        request.P256dh.Should().Be(subscription.P256dh);
        request.Auth.Should().Be(subscription.Auth);

        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Payload);
        payload.Should().Contain("title", "Ranking zaktualizowany");
        payload.Should().Contain("url", "/ranking");
        payload!["body"].Should().Contain("Poland - Germany");

        dbContext.NotificationDeliveries.Should().ContainSingle(delivery =>
            delivery.UserId == user.Id &&
            delivery.PushSubscriptionId == subscription.Id &&
            delivery.MatchId == match.Id &&
            delivery.Type == NotificationType.RankingUpdated &&
            delivery.Status == NotificationDeliveryStatus.Sent &&
            delivery.SentAtUtc == dateTimeProvider.UtcNow);
    }

    [Fact]
    public async Task SendTestNotificationAsync_WithCurrentUserSubscription_ShouldSendOnlyToThatUser()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 15, 23, 10, 0, DateTimeKind.Utc) };
        var currentUser = CreateUser("current@test.local", "Current User");
        var otherUser = CreateUser("other@test.local", "Other User");
        var currentSubscription = CreateSubscription(currentUser.Id, "https://push.example/current-test");
        var otherSubscription = CreateSubscription(otherUser.Id, "https://push.example/other-test");

        dbContext.Users.AddRange(currentUser, otherUser);
        dbContext.PushSubscriptions.AddRange(currentSubscription, otherSubscription);
        await dbContext.SaveChangesAsync();

        var sender = new FakeWebPushSender();
        var service = new WebPushNotificationService(
            dbContext,
            dateTimeProvider,
            Options.Create(new WebPushOptions
            {
                PublicKey = "public-key",
                PrivateKey = "private-key",
                Subject = "mailto:test@example.com",
            }),
            sender,
            NullLogger<WebPushNotificationService>.Instance);

        var response = await service.SendTestNotificationAsync(currentUser.Id);

        response.Should().BeEquivalentTo(new TestNotificationResponse(Attempted: 1, Sent: 1, Failed: 0, Revoked: 0));
        sender.Requests.Should().ContainSingle(request => request.Endpoint == currentSubscription.Endpoint);
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(sender.Requests.Single().Payload);
        payload.Should().Contain("title", "Test powiadomień");
        payload.Should().Contain("url", "/profile");
        dbContext.NotificationDeliveries.Should().ContainSingle(delivery =>
            delivery.UserId == currentUser.Id &&
            delivery.PushSubscriptionId == currentSubscription.Id &&
            delivery.Type == NotificationType.Test &&
            delivery.SubjectKey == $"test:{currentUser.Id:N}" &&
            delivery.Status == NotificationDeliveryStatus.Sent);
    }

    [Theory]
    [InlineData(NotificationType.MissingPrediction2h, 120, "Typowanie zamyka sie za 2h")]
    [InlineData(NotificationType.MissingPrediction30m, 30, "Ostatnie 30 minut na typ")]
    public async Task NotifyDueMatchRemindersAsync_WithMissingPredictionReminder_ShouldSendOnlyToUsersWithoutPrediction(
        NotificationType expectedType,
        int minutesUntilKickoff,
        string expectedTitle)
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc) };
        var homeTeam = CreateTeam("Poland", "POL");
        var awayTeam = CreateTeam("Germany", "GER");
        var match = CreateMatch(homeTeam.Id, awayTeam.Id);
        match.KickoffTimeUtc = dateTimeProvider.UtcNow.AddMinutes(minutesUntilKickoff);
        match.Status = MatchStatus.Scheduled;
        match.IsSettled = false;
        match.HomeScore90 = null;
        match.AwayScore90 = null;
        var missingUser = CreateUser("missing@test.local", "Missing");
        var predictedUser = CreateUser("predicted@test.local", "Predicted");
        var optedOutUser = CreateUser("optedout@test.local", "Opted Out");
        var missingSubscription = CreateSubscription(missingUser.Id, "https://push.example/missing");
        var predictedSubscription = CreateSubscription(predictedUser.Id, "https://push.example/predicted");
        var optedOutSubscription = CreateSubscription(optedOutUser.Id, "https://push.example/opted-out");

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        dbContext.Users.AddRange(missingUser, predictedUser, optedOutUser);
        dbContext.Predictions.Add(new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = predictedUser.Id,
            MatchId = match.Id,
            PredictedHomeScore = 2,
            PredictedAwayScore = 1,
            CreatedAtUtc = dateTimeProvider.UtcNow.AddHours(-1),
        });
        dbContext.NotificationPreferences.Add(new NotificationPreference
        {
            UserId = optedOutUser.Id,
            MissingPrediction2hEnabled = expectedType != NotificationType.MissingPrediction2h,
            MissingPrediction30mEnabled = expectedType != NotificationType.MissingPrediction30m,
            UpdatedAtUtc = dateTimeProvider.UtcNow,
        });
        dbContext.PushSubscriptions.AddRange(missingSubscription, predictedSubscription, optedOutSubscription);
        await dbContext.SaveChangesAsync();

        var sender = new FakeWebPushSender();
        var service = CreateWebPushService(dbContext, dateTimeProvider, sender);

        var response = await service.NotifyDueMatchRemindersAsync();

        response.Should().BeEquivalentTo(new TestNotificationResponse(Attempted: 1, Sent: 1, Failed: 0, Revoked: 0));
        sender.Requests.Should().ContainSingle(request => request.Endpoint == missingSubscription.Endpoint);
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(sender.Requests.Single().Payload);
        payload.Should().Contain("title", expectedTitle);
        payload.Should().Contain("url", $"/matches/{match.Id}");
        payload!["body"].Should().Contain("Poland - Germany");

        var scheduledForUtc = expectedType == NotificationType.MissingPrediction2h
            ? match.KickoffTimeUtc.AddHours(-2)
            : match.KickoffTimeUtc.AddMinutes(-30);
        dbContext.NotificationDeliveries.Should().ContainSingle(delivery =>
            delivery.UserId == missingUser.Id &&
            delivery.PushSubscriptionId == missingSubscription.Id &&
            delivery.MatchId == match.Id &&
            delivery.Type == expectedType &&
            delivery.SubjectKey == $"{expectedType}:{match.Id:N}" &&
            delivery.ScheduledForUtc == scheduledForUtc &&
            delivery.Status == NotificationDeliveryStatus.Sent);
    }

    [Fact]
    public async Task NotifyDueMatchRemindersAsync_WhenMorningDigestIsDue_ShouldSendMissingCountForTodaysMatches()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc) };
        var homeTeam = CreateTeam("Poland", "POL");
        var awayTeam = CreateTeam("Germany", "GER");
        var firstMatch = CreateMatch(homeTeam.Id, awayTeam.Id);
        firstMatch.KickoffTimeUtc = new DateTime(2026, 6, 15, 16, 0, 0, DateTimeKind.Utc);
        var secondMatch = CreateMatch(homeTeam.Id, awayTeam.Id);
        secondMatch.KickoffTimeUtc = new DateTime(2026, 6, 15, 19, 0, 0, DateTimeKind.Utc);
        secondMatch.MatchNumber = 2;
        var user = CreateUser();
        var subscription = CreateSubscription(user.Id, "https://push.example/morning");
        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.AddRange(firstMatch, secondMatch);
        dbContext.Users.Add(user);
        dbContext.Predictions.Add(new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MatchId = firstMatch.Id,
            PredictedHomeScore = 1,
            PredictedAwayScore = 1,
            CreatedAtUtc = dateTimeProvider.UtcNow.AddHours(-1),
        });
        dbContext.PushSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        var sender = new FakeWebPushSender();
        var service = CreateWebPushService(dbContext, dateTimeProvider, sender);

        var response = await service.NotifyDueMatchRemindersAsync();

        response.Should().BeEquivalentTo(new TestNotificationResponse(Attempted: 1, Sent: 1, Failed: 0, Revoked: 0));
        sender.Requests.Should().ContainSingle(request => request.Endpoint == subscription.Endpoint);
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(sender.Requests.Single().Payload);
        payload.Should().Contain("title", "Dzisiejsze mecze czekaja");
        payload.Should().Contain("body", "Masz 1 mecz bez typu.");
        payload.Should().Contain("url", "/matches");
        dbContext.NotificationDeliveries.Should().ContainSingle(delivery =>
            delivery.UserId == user.Id &&
            delivery.PushSubscriptionId == subscription.Id &&
            delivery.Type == NotificationType.MorningDigest &&
            delivery.SubjectKey == "digest:2026-06-15" &&
            delivery.Status == NotificationDeliveryStatus.Sent);
    }

    [Fact]
    public async Task NotifyDueMatchRemindersAsync_WhenDeliveryAlreadyExists_ShouldNotSendDuplicate()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider { UtcNow = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc) };
        var homeTeam = CreateTeam("Poland", "POL");
        var awayTeam = CreateTeam("Germany", "GER");
        var match = CreateMatch(homeTeam.Id, awayTeam.Id);
        match.KickoffTimeUtc = dateTimeProvider.UtcNow.AddHours(2);
        match.Status = MatchStatus.Scheduled;
        match.IsSettled = false;
        match.HomeScore90 = null;
        match.AwayScore90 = null;
        var user = CreateUser();
        var subscription = CreateSubscription(user.Id, "https://push.example/dedupe");
        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        dbContext.Users.Add(user);
        dbContext.PushSubscriptions.Add(subscription);
        dbContext.NotificationDeliveries.Add(new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PushSubscriptionId = subscription.Id,
            MatchId = match.Id,
            SubjectKey = $"{NotificationType.MissingPrediction2h}:{match.Id:N}",
            Type = NotificationType.MissingPrediction2h,
            ScheduledForUtc = match.KickoffTimeUtc.AddHours(-2),
            SentAtUtc = dateTimeProvider.UtcNow.AddMinutes(-1),
            Status = NotificationDeliveryStatus.Sent,
            CreatedAtUtc = dateTimeProvider.UtcNow.AddMinutes(-1),
        });
        await dbContext.SaveChangesAsync();

        var sender = new FakeWebPushSender();
        var service = CreateWebPushService(dbContext, dateTimeProvider, sender);

        var response = await service.NotifyDueMatchRemindersAsync();

        response.Should().BeEquivalentTo(new TestNotificationResponse(Attempted: 0, Sent: 0, Failed: 0, Revoked: 0));
        sender.Requests.Should().BeEmpty();
        dbContext.NotificationDeliveries.Should().ContainSingle();
    }

    private static ApplicationUser CreateUser(
        string email = "player@test.local",
        string displayName = "Player") =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private static PushSubscription CreateSubscription(Guid userId, string endpoint) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Endpoint = endpoint,
            P256dh = "key",
            Auth = "auth",
            CreatedAtUtc = DateTime.UtcNow,
            LastSeenAtUtc = DateTime.UtcNow,
        };

    private static Team CreateTeam(string name, string shortName) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            ShortName = shortName,
            CountryCode = shortName,
        };

    private static Match CreateMatch(Guid homeTeamId, Guid awayTeamId) =>
        new()
        {
            Id = Guid.NewGuid(),
            MatchNumber = 1,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            KickoffTimeUtc = new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc),
            HomeScore90 = 2,
            AwayScore90 = 1,
            Status = MatchStatus.Settled,
            IsSettled = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private static WebPushNotificationService CreateWebPushService(
        WorldCupTyper.Infrastructure.Persistence.WorldCupTyperDbContext dbContext,
        TestDateTimeProvider dateTimeProvider,
        FakeWebPushSender sender) =>
        new(
            dbContext,
            dateTimeProvider,
            Options.Create(new WebPushOptions
            {
                PublicKey = "public-key",
                PrivateKey = "private-key",
                Subject = "mailto:test@example.com",
            }),
            sender,
            NullLogger<WebPushNotificationService>.Instance);

    private sealed class FakeWebPushSender : IWebPushSender
    {
        public List<WebPushRequest> Requests { get; } = [];

        public Task SendAsync(WebPushRequest request, WebPushOptions options, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }
}
