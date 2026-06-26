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
            .ThenInclude(match => match.HomeTeam)
            .Include(snapshot => snapshot.Match)
            .ThenInclude(match => match.AwayTeam)
            .OrderBy(snapshot => snapshot.Match.KickoffTimeUtc)
            .ThenBy(snapshot => snapshot.CreatedAtUtc)
            .ThenBy(snapshot => snapshot.Match.MatchNumber)
            .ToListAsync(cancellationToken);

        return snapshots.Select(snapshot => new RankingProgressPointDto(
            snapshot.MatchId,
            snapshot.Match.MatchNumber,
            BuildMatchLabel(snapshot),
            snapshot.CreatedAtUtc,
            snapshot.TotalPoints,
            snapshot.ExactScoreHits,
            snapshot.CorrectOutcomeHits,
            snapshot.PredictionsCount,
            snapshot.Position))
            .ToList();
    }

    public async Task<IReadOnlyCollection<RankingProgressSeriesDto>> GetProgressForRankingAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var snapshots = await _dbContext.LeaderboardSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.User.IsActive)
            .Include(snapshot => snapshot.User)
            .Include(snapshot => snapshot.Match)
            .ThenInclude(match => match.HomeTeam)
            .Include(snapshot => snapshot.Match)
            .ThenInclude(match => match.AwayTeam)
            .OrderBy(snapshot => snapshot.Match.KickoffTimeUtc)
            .ThenBy(snapshot => snapshot.CreatedAtUtc)
            .ThenBy(snapshot => snapshot.Match.MatchNumber)
            .ThenBy(snapshot => snapshot.Position)
            .ToListAsync(cancellationToken);

        return snapshots
            .GroupBy(snapshot => snapshot.UserId)
            .Select(group =>
            {
                var orderedSnapshots = group
                    .OrderBy(snapshot => snapshot.Match.KickoffTimeUtc)
                    .ThenBy(snapshot => snapshot.CreatedAtUtc)
                    .ThenBy(snapshot => snapshot.Match.MatchNumber)
                    .ToList();
                var latestSnapshot = orderedSnapshots.Last();
                var user = latestSnapshot.User;

                return new
                {
                    LatestPosition = latestSnapshot.Position,
                    user.DisplayName,
                    Series = new RankingProgressSeriesDto(
                        user.Id,
                        user.DisplayName,
                        user.AvatarUrl,
                        currentUserId.HasValue && currentUserId.Value == user.Id,
                        orderedSnapshots.Select(snapshot => new RankingProgressPointDto(
                            snapshot.MatchId,
                            snapshot.Match.MatchNumber,
                            BuildMatchLabel(snapshot),
                            snapshot.CreatedAtUtc,
                            snapshot.TotalPoints,
                            snapshot.ExactScoreHits,
                            snapshot.CorrectOutcomeHits,
                            snapshot.PredictionsCount,
                            snapshot.Position))
                            .ToList())
                };
            })
            .OrderBy(entry => entry.LatestPosition)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.Series)
            .ToList();
    }

    private static string BuildMatchLabel(WorldCupTyper.Domain.Entities.LeaderboardSnapshot snapshot)
    {
        var home = snapshot.Match.HomeTeam.ShortName.Trim();
        var away = snapshot.Match.AwayTeam.ShortName.Trim();

        return string.IsNullOrWhiteSpace(home) || string.IsNullOrWhiteSpace(away)
            ? $"M{snapshot.Match.MatchNumber}"
            : $"{home}-{away}";
    }
}
