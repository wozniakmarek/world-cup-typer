using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Infrastructure.FootballData;

public sealed class FootballDataSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FootballDataOptions _options;
    private readonly ILogger<FootballDataSyncWorker> _logger;

    public FootballDataSyncWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<FootballDataOptions> options,
        ILogger<FootballDataSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunOnceIfEnabledAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var importService = scope.ServiceProvider.GetRequiredService<IScheduleImportService>();
            await importService.ImportScheduleAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Football-data.org sync failed.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiToken))
        {
            _logger.LogInformation("Football-data.org sync worker is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceIfEnabledAsync(stoppingToken);

            var delay = TimeSpan.FromMinutes(Math.Max(1, _options.SyncIntervalMinutes));
            await Task.Delay(delay, stoppingToken);
        }
    }
}
