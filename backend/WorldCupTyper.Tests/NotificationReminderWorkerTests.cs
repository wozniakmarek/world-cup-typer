using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Infrastructure.Options;
using WorldCupTyper.Infrastructure.Services;

namespace WorldCupTyper.Tests;

public sealed class NotificationReminderWorkerTests
{
    [Fact]
    public async Task RunOnceAsync_WhenEnabled_ShouldInvokeNotificationService()
    {
        var notificationService = new FakeNotificationService();
        using var provider = CreateProvider(notificationService);
        var worker = new NotificationReminderWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new NotificationReminderOptions { Enabled = true }),
            NullLogger<NotificationReminderWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        notificationService.DueReminderCalls.Should().Be(1);
    }

    [Fact]
    public async Task RunOnceAsync_WhenDisabled_ShouldSkipNotificationService()
    {
        var notificationService = new FakeNotificationService();
        using var provider = CreateProvider(notificationService);
        var worker = new NotificationReminderWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new NotificationReminderOptions { Enabled = false }),
            NullLogger<NotificationReminderWorker>.Instance);

        await worker.RunOnceAsync(CancellationToken.None);

        notificationService.DueReminderCalls.Should().Be(0);
    }

    private static ServiceProvider CreateProvider(FakeNotificationService notificationService)
    {
        var services = new ServiceCollection();
        services.AddScoped<INotificationService>(_ => notificationService);
        return services.BuildServiceProvider();
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public int DueReminderCalls { get; private set; }

        public Task NotifyPredictionsClosingSoonAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TestNotificationResponse> NotifyDueMatchRemindersAsync(CancellationToken cancellationToken = default)
        {
            DueReminderCalls++;
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
}
