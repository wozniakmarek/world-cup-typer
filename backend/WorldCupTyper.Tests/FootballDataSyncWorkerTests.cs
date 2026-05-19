using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Infrastructure.FootballData;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Tests;

public sealed class FootballDataSyncWorkerTests
{
    [Fact]
    public async Task RunOnceIfEnabledAsync_WhenDisabled_ShouldNotCallImportService()
    {
        var importService = new CountingScheduleImportService();
        var worker = CreateWorker(importService, new FootballDataOptions
        {
            Enabled = false,
            ApiToken = "secret-token",
        });

        await worker.RunOnceIfEnabledAsync(CancellationToken.None);

        importService.Calls.Should().Be(0);
    }

    [Fact]
    public async Task RunOnceIfEnabledAsync_WhenTokenBlank_ShouldNotCallImportService()
    {
        var importService = new CountingScheduleImportService();
        var worker = CreateWorker(importService, new FootballDataOptions
        {
            Enabled = true,
            ApiToken = "",
        });

        await worker.RunOnceIfEnabledAsync(CancellationToken.None);

        importService.Calls.Should().Be(0);
    }

    [Fact]
    public async Task RunOnceIfEnabledAsync_WhenEnabledAndTokenConfigured_ShouldCallImportService()
    {
        var importService = new CountingScheduleImportService();
        var worker = CreateWorker(importService, new FootballDataOptions
        {
            Enabled = true,
            ApiToken = "secret-token",
        });

        await worker.RunOnceIfEnabledAsync(CancellationToken.None);

        importService.Calls.Should().Be(1);
    }

    private static FootballDataSyncWorker CreateWorker(CountingScheduleImportService importService, FootballDataOptions options)
    {
        var services = new ServiceCollection();
        services.AddScoped<IScheduleImportService>(_ => importService);
        var provider = services.BuildServiceProvider();

        return new FootballDataSyncWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<FootballDataSyncWorker>.Instance);
    }

    private sealed class CountingScheduleImportService : IScheduleImportService
    {
        public int Calls { get; private set; }

        public Task<ScheduleSyncSummaryDto> ImportScheduleAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new ScheduleSyncSummaryDto(0, 0, 0, 0, 0));
        }
    }
}
