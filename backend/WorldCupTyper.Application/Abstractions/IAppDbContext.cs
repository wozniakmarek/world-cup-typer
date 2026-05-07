using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<Team> Teams { get; }
    DbSet<Match> Matches { get; }
    DbSet<Prediction> Predictions { get; }
    DbSet<PredictionResult> PredictionResults { get; }
    DbSet<LeaderboardSnapshot> LeaderboardSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
