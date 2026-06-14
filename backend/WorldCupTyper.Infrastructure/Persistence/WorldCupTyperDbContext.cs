using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Persistence;

public sealed class WorldCupTyperDbContext : DbContext, IAppDbContext
{
    public WorldCupTyperDbContext(DbContextOptions<WorldCupTyperDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<PredictionResult> PredictionResults => Set<PredictionResult>();
    public DbSet<LeaderboardSnapshot> LeaderboardSnapshots => Set<LeaderboardSnapshot>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorldCupTyperDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
