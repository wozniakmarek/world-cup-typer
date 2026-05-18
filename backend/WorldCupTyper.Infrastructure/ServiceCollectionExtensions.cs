using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Infrastructure.Auth;
using WorldCupTyper.Infrastructure.Options;
using WorldCupTyper.Infrastructure.Persistence;
using WorldCupTyper.Infrastructure.Seeding;
using WorldCupTyper.Infrastructure.Services;
using WorldCupTyper.Infrastructure.Time;

namespace WorldCupTyper.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorldCupTyperInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<DevelopmentSeedOptions>(configuration.GetSection(DevelopmentSeedOptions.SectionName));
        services.Configure<DatabaseStartupOptions>(configuration.GetSection(DatabaseStartupOptions.SectionName));

        services.AddDbContext<WorldCupTyperDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<WorldCupTyperDbContext>());

        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<INotificationService, NoopNotificationService>();
        services.AddScoped<IScheduleImportService, StubScheduleImportService>();
        services.AddScoped<IKnockoutResolverService, StubKnockoutResolverService>();

        services.AddScoped<LeaderboardBuilder>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlayerService, PlayerService>();
        services.AddScoped<ITeamService, TeamService>();
        services.AddScoped<IMatchService, MatchService>();
        services.AddScoped<IPredictionService, PredictionService>();
        services.AddScoped<IScoringService, ScoringService>();
        services.AddScoped<IRankingService, RankingService>();
        services.AddScoped<IMatchSettlementService, MatchSettlementService>();

        services.AddScoped<DevelopmentDataSeeder>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
