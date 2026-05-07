using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Application.Services;

public sealed class RankingService : IRankingService
{
    private readonly LeaderboardBuilder _leaderboardBuilder;
    private readonly IAppDbContext _dbContext;

    public RankingService(LeaderboardBuilder leaderboardBuilder, IAppDbContext dbContext)
    {
        _leaderboardBuilder = leaderboardBuilder;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<LeaderboardEntryDto>> GetRankingAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        return await _leaderboardBuilder.BuildAsync(currentUserId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LeaderboardEntryDto>> GetTopAsync(int count = 5, Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var ranking = await _leaderboardBuilder.BuildAsync(currentUserId, cancellationToken);
        return ranking.Take(count).ToList();
    }

    public async Task<LeaderboardEntryDto> GetUserRankingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var ranking = await _leaderboardBuilder.BuildAsync(userId, cancellationToken);
        var entry = ranking.FirstOrDefault(candidate => candidate.UserId == userId);
        if (entry is null)
        {
            throw new NotFoundException("Nie znaleziono pozycji użytkownika w rankingu.");
        }

        return entry;
    }

    public async Task<IReadOnlyCollection<RankingProgressPointDto>> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var snapshots = await _dbContext.LeaderboardSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.UserId == userId)
            .Include(snapshot => snapshot.Match)
            .OrderBy(snapshot => snapshot.Match.KickoffTimeUtc)
            .ThenBy(snapshot => snapshot.Match.MatchNumber)
            .ToListAsync(cancellationToken);

        return snapshots.Select(snapshot => new RankingProgressPointDto(
            snapshot.MatchId,
            snapshot.Match.MatchNumber,
            snapshot.CreatedAtUtc,
            snapshot.TotalPoints,
            snapshot.ExactScoreHits,
            snapshot.CorrectOutcomeHits,
            snapshot.PredictionsCount,
            snapshot.Position))
            .ToList();
    }
}
