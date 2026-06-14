using FluentAssertions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
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
}
