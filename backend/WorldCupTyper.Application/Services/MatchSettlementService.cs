using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Application.Services;

public sealed class MatchSettlementService : IMatchSettlementService
{
    private readonly IAppDbContext _dbContext;
    private readonly IScoringService _scoringService;
    private readonly LeaderboardBuilder _leaderboardBuilder;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MatchSettlementService(
        IAppDbContext dbContext,
        IScoringService scoringService,
        LeaderboardBuilder leaderboardBuilder,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _scoringService = scoringService;
        _leaderboardBuilder = leaderboardBuilder;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task SettleMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return SettleMatchInternalAsync(matchId, false, cancellationToken);
    }

    public async Task RecalculateRankingsAsync(CancellationToken cancellationToken = default)
    {
        var snapshots = await _dbContext.LeaderboardSnapshots.ToListAsync(cancellationToken);
        _dbContext.LeaderboardSnapshots.RemoveRange(snapshots);

        var matchesToReset = await _dbContext.Matches
            .Where(match => match.IsSettled || (match.HomeScore90.HasValue && match.AwayScore90.HasValue))
            .ToListAsync(cancellationToken);

        foreach (var match in matchesToReset)
        {
            match.IsSettled = false;
            match.SettledAtUtc = null;
            if (match.HomeScore90.HasValue && match.AwayScore90.HasValue)
            {
                match.Status = MatchStatus.Finished;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_dbContext is DbContext dbContext)
        {
            dbContext.ChangeTracker.Clear();
        }

        var matchIds = matchesToReset
            .Where(match => match.HomeScore90.HasValue && match.AwayScore90.HasValue)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .Select(match => match.Id)
            .ToList();

        foreach (var matchId in matchIds)
        {
            await SettleMatchInternalAsync(matchId, true, cancellationToken);
        }
    }

    private async Task SettleMatchInternalAsync(Guid matchId, bool force, CancellationToken cancellationToken)
    {
        var match = await _dbContext.Matches
            .Include(candidate => candidate.Predictions)
            .ThenInclude(prediction => prediction.Result)
            .FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new NotFoundException("Nie znaleziono meczu.");
        }

        if (!match.HomeScore90.HasValue || !match.AwayScore90.HasValue)
        {
            throw new BusinessRuleException("Nie można rozliczyć meczu bez wyniku po 90 minutach.");
        }

        if (match.IsSettled && !force)
        {
            throw new BusinessRuleException("Mecz został już rozliczony.");
        }

        var existingSnapshots = await _dbContext.LeaderboardSnapshots
            .Where(snapshot => snapshot.MatchId == matchId)
            .ToListAsync(cancellationToken);

        if (existingSnapshots.Count > 0)
        {
            _dbContext.LeaderboardSnapshots.RemoveRange(existingSnapshots);
        }

        var calculatedAtUtc = _dateTimeProvider.UtcNow;
        foreach (var prediction in match.Predictions)
        {
            var score = _scoringService.CalculateScore(
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                match.HomeScore90.Value,
                match.AwayScore90.Value);

            prediction.LockedAtUtc ??= match.KickoffTimeUtc;
            var result = prediction.Result ?? new PredictionResult
            {
                Id = Guid.NewGuid(),
                PredictionId = prediction.Id,
            };

            result.Points = score.Points;
            result.IsExactScore = score.IsExactScore;
            result.IsCorrectOutcome = score.IsCorrectOutcome;
            result.CalculatedAtUtc = calculatedAtUtc;

            if (prediction.Result is null)
            {
                prediction.Result = result;
                if (_dbContext is DbContext dbContext)
                {
                    dbContext.Entry(result).State = EntityState.Added;
                }
            }
        }

        match.IsSettled = true;
        match.SettledAtUtc = calculatedAtUtc;
        match.Status = MatchStatus.Settled;
        match.WinnerTeamId = ResolveWinner(match);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ranking = await _leaderboardBuilder.BuildAsync(null, cancellationToken);
        var snapshotsToCreate = ranking.Select(entry => new LeaderboardSnapshot
        {
            Id = Guid.NewGuid(),
            MatchId = match.Id,
            UserId = entry.UserId,
            TotalPoints = entry.TotalPoints,
            ExactScoreHits = entry.ExactScoreHits,
            CorrectOutcomeHits = entry.CorrectOutcomeHits,
            PredictionsCount = entry.PredictionsCount,
            Position = entry.Position,
            CreatedAtUtc = calculatedAtUtc,
        });

        await _dbContext.LeaderboardSnapshots.AddRangeAsync(snapshotsToCreate, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Guid? ResolveWinner(Match match)
    {
        var homeScore = match.HomeScoreFinal ?? match.HomeScore90;
        var awayScore = match.AwayScoreFinal ?? match.AwayScore90;

        if (!homeScore.HasValue || !awayScore.HasValue || homeScore == awayScore)
        {
            return null;
        }

        return homeScore > awayScore ? match.HomeTeamId : match.AwayTeamId;
    }
}
