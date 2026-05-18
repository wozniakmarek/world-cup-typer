using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Infrastructure.Options;
using WorldCupTyper.Infrastructure.Persistence;

namespace WorldCupTyper.Infrastructure.Seeding;

public sealed class DatabaseInitializer
{
    private readonly WorldCupTyperDbContext _dbContext;
    private readonly DevelopmentDataSeeder _developmentDataSeeder;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IMatchSettlementService _matchSettlementService;
    private readonly DatabaseStartupOptions _databaseStartupOptions;

    public DatabaseInitializer(
        WorldCupTyperDbContext dbContext,
        DevelopmentDataSeeder developmentDataSeeder,
        IHostEnvironment hostEnvironment,
        IMatchSettlementService matchSettlementService,
        IOptions<DatabaseStartupOptions> databaseStartupOptions)
    {
        _dbContext = dbContext;
        _developmentDataSeeder = developmentDataSeeder;
        _hostEnvironment = hostEnvironment;
        _matchSettlementService = matchSettlementService;
        _databaseStartupOptions = databaseStartupOptions.Value;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_databaseStartupOptions.ApplyMigrationsOnStartup)
        {
            await _dbContext.Database.MigrateAsync(cancellationToken);
        }

        if (_hostEnvironment.IsDevelopment())
        {
            var rankingNeedsRebuild = await _developmentDataSeeder.SeedAsync(cancellationToken);
            if (rankingNeedsRebuild)
            {
                await _matchSettlementService.RecalculateRankingsAsync(cancellationToken);
            }
        }
    }
}
