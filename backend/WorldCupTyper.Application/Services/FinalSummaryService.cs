using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Services;

public sealed class FinalSummaryService : IFinalSummaryService
{
    private readonly IAppDbContext _dbContext;

    public FinalSummaryService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinalSummaryResponseDto> GetFinalSummaryAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var activeUsers = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

        var activeUserIds = activeUsers.Select(user => user.Id).ToList();
        var settledMatches = await _dbContext.Matches
            .AsNoTracking()
            .Where(match => match.IsSettled)
            .Include(match => match.HomeTeam)
            .Include(match => match.AwayTeam)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);

        var settledMatchIds = settledMatches.Select(match => match.Id).ToList();
        var snapshots = await LoadSnapshotsAsync(activeUserIds, settledMatchIds, cancellationToken);
        var predictionRows = await LoadPredictionRowsAsync(activeUserIds, settledMatchIds, cancellationToken);
        var predictionStatsByUser = BuildPredictionStatsByUser(predictionRows);
        var seriesData = BuildPositionSeries(snapshots, currentUserId);
        var positionSeries = seriesData.Select(data => data.Series).ToList();
        var finalTop = BuildFinalTop(activeUsers, predictionStatsByUser, currentUserId);
        var finalLeader = finalTop.FirstOrDefault();

        return new FinalSummaryResponseDto(
            new FinalSummaryStatsDto(
                settledMatches.Count,
                activeUsers.Count,
                finalLeader?.UserId,
                finalLeader?.DisplayName),
            positionSeries,
            finalTop,
            BuildGlobalFacts(seriesData, predictionRows, settledMatches));
    }

    public async Task<PersonalFinalSummaryResponseDto> GetPersonalFinalSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var summary = await GetFinalSummaryAsync(userId, cancellationToken);
        var finalEntry = summary.FinalTop.FirstOrDefault(entry => entry.UserId == userId);
        if (finalEntry is not null)
        {
            return new PersonalFinalSummaryResponseDto(
                finalEntry.UserId,
                finalEntry.DisplayName,
                finalEntry.AvatarUrl,
                finalEntry.FinalPosition,
                finalEntry.TotalPoints,
                finalEntry.ExactScoreHits,
                finalEntry.CorrectOutcomeHits,
                finalEntry.PredictionsCount,
                Array.Empty<FinalSummaryFactDto>(),
                Array.Empty<Guid>());
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        return new PersonalFinalSummaryResponseDto(
            user.Id,
            user.DisplayName,
            user.AvatarUrl,
            0,
            0,
            0,
            0,
            0,
            Array.Empty<FinalSummaryFactDto>(),
            Array.Empty<Guid>());
    }

    private async Task<List<LeaderboardSnapshot>> LoadSnapshotsAsync(
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> settledMatchIds,
        CancellationToken cancellationToken)
    {
        if (activeUserIds.Count == 0 || settledMatchIds.Count == 0)
        {
            return new List<LeaderboardSnapshot>();
        }

        return await _dbContext.LeaderboardSnapshots
            .AsNoTracking()
            .Where(snapshot => activeUserIds.Contains(snapshot.UserId) && settledMatchIds.Contains(snapshot.MatchId))
            .Include(snapshot => snapshot.User)
            .Include(snapshot => snapshot.Match)
            .ThenInclude(match => match.HomeTeam)
            .Include(snapshot => snapshot.Match)
            .ThenInclude(match => match.AwayTeam)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<PredictionSummaryRow>> LoadPredictionRowsAsync(
        IReadOnlyCollection<Guid> activeUserIds,
        IReadOnlyCollection<Guid> settledMatchIds,
        CancellationToken cancellationToken)
    {
        if (activeUserIds.Count == 0 || settledMatchIds.Count == 0)
        {
            return new List<PredictionSummaryRow>();
        }

        var rows = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction =>
                activeUserIds.Contains(prediction.UserId)
                && settledMatchIds.Contains(prediction.MatchId)
                && prediction.Result != null)
            .Select(prediction => new
            {
                prediction.UserId,
                prediction.MatchId,
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                prediction.Result!.Points,
                prediction.Result.IsExactScore,
                prediction.Result.IsCorrectOutcome,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new PredictionSummaryRow(
                row.UserId,
                row.MatchId,
                row.PredictedHomeScore,
                row.PredictedAwayScore,
                row.Points,
                row.IsExactScore,
                row.IsCorrectOutcome))
            .ToList();
    }

    private static Dictionary<Guid, PlayerStats> BuildPredictionStatsByUser(IReadOnlyCollection<PredictionSummaryRow> predictionRows)
    {
        return predictionRows
            .GroupBy(row => row.UserId)
            .ToDictionary(
                group => group.Key,
                group => new PlayerStats(
                    group.Sum(row => row.Points),
                    group.Count(row => row.IsExactScore),
                    group.Count(row => row.IsCorrectOutcome),
                    group.Count()));
    }

    private static List<FinalPositionSeriesData> BuildPositionSeries(IReadOnlyCollection<LeaderboardSnapshot> snapshots, Guid? currentUserId)
    {
        return snapshots
            .GroupBy(snapshot => snapshot.UserId)
            .Select(group =>
            {
                var orderedSnapshots = group
                    .OrderBy(snapshot => snapshot.Match.KickoffTimeUtc)
                    .ThenBy(snapshot => snapshot.Match.MatchNumber)
                    .ThenBy(snapshot => snapshot.CreatedAtUtc)
                    .ToList();
                var finalSnapshot = orderedSnapshots.Last();
                var user = finalSnapshot.User;

                var series = new FinalRankingPositionSeriesDto(
                    user.Id,
                    user.DisplayName,
                    user.AvatarUrl,
                    finalSnapshot.Position,
                    finalSnapshot.TotalPoints,
                    currentUserId.HasValue && currentUserId.Value == user.Id,
                    orderedSnapshots
                        .Select(snapshot => new FinalRankingPositionPointDto(
                            snapshot.MatchId,
                            snapshot.Match.MatchNumber,
                            BuildMatchLabel(snapshot.Match),
                            snapshot.CreatedAtUtc,
                            snapshot.Position,
                            snapshot.TotalPoints))
                        .ToList());

                return new FinalPositionSeriesData(series, finalSnapshot);
            })
            .OrderBy(data => data.Series.FinalPosition)
            .ThenBy(data => data.Series.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<FinalRankingEntryDto> BuildFinalTop(
        IReadOnlyCollection<ApplicationUser> activeUsers,
        IReadOnlyDictionary<Guid, PlayerStats> predictionStatsByUser,
        Guid? currentUserId)
    {
        return activeUsers
            .Select(user =>
            {
                var stats = predictionStatsByUser.TryGetValue(user.Id, out var predictionStats)
                    ? predictionStats
                    : new PlayerStats(0, 0, 0, 0);

                return new
                {
                    user.Id,
                    user.DisplayName,
                    user.AvatarUrl,
                    Stats = stats,
                };
            })
            .OrderByDescending(entry => entry.Stats.TotalPoints)
            .ThenByDescending(entry => entry.Stats.ExactScoreHits)
            .ThenByDescending(entry => entry.Stats.CorrectOutcomeHits)
            .ThenByDescending(entry => entry.Stats.PredictionsCount)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select((entry, index) => new FinalRankingEntryDto(
                entry.Id,
                entry.DisplayName,
                entry.AvatarUrl,
                index + 1,
                entry.Stats.TotalPoints,
                entry.Stats.ExactScoreHits,
                entry.Stats.CorrectOutcomeHits,
                entry.Stats.PredictionsCount,
                currentUserId.HasValue && currentUserId.Value == entry.Id))
            .ToList();
    }

    private static List<FinalSummaryFactDto> BuildGlobalFacts(
        IReadOnlyCollection<FinalPositionSeriesData> seriesData,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var facts = new List<FinalSummaryFactDto>();
        AddBiggestClimbFact(facts, seriesData);
        AddBiggestDropFact(facts, seriesData);
        AddMostExactMatchFact(facts, predictionRows, settledMatches);
        AddDrawSpecialistFact(facts, predictionRows, settledMatches, seriesData);
        return facts;
    }

    private static void AddBiggestClimbFact(List<FinalSummaryFactDto> facts, IReadOnlyCollection<FinalPositionSeriesData> seriesData)
    {
        var movements = seriesData
            .Select(data => new PlayerMovement(
                data.Series.UserId,
                data.Series.DisplayName,
                data.Series.Points.First().Position,
                data.Series.FinalPosition))
            .ToList();

        var biggestClimb = movements.Count == 0
            ? 0
            : movements.Max(movement => movement.FirstPosition - movement.FinalPosition);

        if (biggestClimb <= 0)
        {
            return;
        }

        var winners = movements
            .Where(movement => movement.FirstPosition - movement.FinalPosition == biggestClimb)
            .OrderBy(movement => movement.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "biggest-climb",
            "Najwiekszy awans",
            $"Awans o {biggestClimb} miejsc",
            $"{JoinNames(winners.Select(winner => winner.DisplayName))} najmocniej poprawili pozycje od pierwszego do finalnego snapshotu.",
            winners.Select(winner => winner.UserId).ToList(),
            Array.Empty<Guid>()));
    }

    private static void AddBiggestDropFact(List<FinalSummaryFactDto> facts, IReadOnlyCollection<FinalPositionSeriesData> seriesData)
    {
        var movements = seriesData
            .Select(data => new PlayerMovement(
                data.Series.UserId,
                data.Series.DisplayName,
                data.Series.Points.First().Position,
                data.Series.FinalPosition))
            .ToList();

        var biggestDrop = movements.Count == 0
            ? 0
            : movements.Max(movement => movement.FinalPosition - movement.FirstPosition);

        if (biggestDrop <= 0)
        {
            return;
        }

        var players = movements
            .Where(movement => movement.FinalPosition - movement.FirstPosition == biggestDrop)
            .OrderBy(movement => movement.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "biggest-drop",
            "Najwiekszy spadek",
            $"Spadek o {biggestDrop} miejsc",
            $"{JoinNames(players.Select(player => player.DisplayName))} stracili najwiecej miejsc od pierwszego do finalnego snapshotu.",
            players.Select(player => player.UserId).ToList(),
            Array.Empty<Guid>()));
    }

    private static void AddMostExactMatchFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var exactMatchCounts = predictionRows
            .Where(row => row.IsExactScore)
            .GroupBy(row => row.MatchId)
            .Select(group => new
            {
                MatchId = group.Key,
                ExactHits = group.Count(),
            })
            .Where(entry => entry.ExactHits > 0)
            .ToList();

        if (exactMatchCounts.Count == 0)
        {
            return;
        }

        var maxExactHits = exactMatchCounts.Max(entry => entry.ExactHits);
        var matchIds = exactMatchCounts
            .Where(entry => entry.ExactHits == maxExactHits)
            .Select(entry => entry.MatchId)
            .Where(matchById.ContainsKey)
            .OrderBy(matchId => matchById[matchId].MatchNumber)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "most-exact-match",
            "Najwiecej dokladnych wynikow",
            $"Najlatwiejszy dokladny wynik: {maxExactHits}",
            $"{JoinNames(matchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))} mial najwiecej dokladnych typow.",
            Array.Empty<Guid>(),
            matchIds));
    }

    private static void AddDrawSpecialistFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches,
        IReadOnlyCollection<FinalPositionSeriesData> seriesData)
    {
        var drawMatchIds = settledMatches
            .Where(match => match.HomeScore90.HasValue && match.AwayScore90.HasValue && match.HomeScore90.Value == match.AwayScore90.Value)
            .Select(match => match.Id)
            .ToHashSet();

        if (drawMatchIds.Count == 0)
        {
            return;
        }

        var drawHitsByUser = predictionRows
            .Where(row =>
                drawMatchIds.Contains(row.MatchId)
                && row.IsCorrectOutcome
                && row.PredictedHomeScore == row.PredictedAwayScore)
            .GroupBy(row => row.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Hits = group.Count(),
            })
            .Where(entry => entry.Hits > 0)
            .ToList();

        if (drawHitsByUser.Count == 0)
        {
            return;
        }

        var userNameById = seriesData.ToDictionary(data => data.Series.UserId, data => data.Series.DisplayName);
        var maxHits = drawHitsByUser.Max(entry => entry.Hits);
        var userIds = drawHitsByUser
            .Where(entry => entry.Hits == maxHits)
            .Select(entry => entry.UserId)
            .OrderBy(userId => userNameById.TryGetValue(userId, out var displayName) ? displayName : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var userIdSet = userIds.ToHashSet();
        var relatedMatchIds = predictionRows
            .Where(row =>
                userIdSet.Contains(row.UserId)
                && drawMatchIds.Contains(row.MatchId)
                && row.IsCorrectOutcome
                && row.PredictedHomeScore == row.PredictedAwayScore)
            .Select(row => row.MatchId)
            .Distinct()
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "draw-specialist",
            "Specjalista od remisow",
            $"Trafione remisy: {maxHits}",
            $"{JoinNames(userIds.Select(userId => userNameById.TryGetValue(userId, out var displayName) ? displayName : "Gracz"))} najlepiej czytali remisy.",
            userIds,
            relatedMatchIds));
    }

    private static string BuildMatchLabel(Match match)
    {
        var home = match.HomeTeam.ShortName.Trim();
        var away = match.AwayTeam.ShortName.Trim();

        return string.IsNullOrWhiteSpace(home) || string.IsNullOrWhiteSpace(away)
            ? $"M{match.MatchNumber}"
            : $"{home}-{away}";
    }

    private static string JoinNames(IEnumerable<string> names)
    {
        return string.Join(", ", names);
    }

    private sealed record FinalPositionSeriesData(FinalRankingPositionSeriesDto Series, LeaderboardSnapshot FinalSnapshot);

    private sealed record PlayerStats(int TotalPoints, int ExactScoreHits, int CorrectOutcomeHits, int PredictionsCount);

    private sealed record PredictionSummaryRow(
        Guid UserId,
        Guid MatchId,
        int PredictedHomeScore,
        int PredictedAwayScore,
        int Points,
        bool IsExactScore,
        bool IsCorrectOutcome);

    private sealed record PlayerMovement(Guid UserId, string DisplayName, int FirstPosition, int FinalPosition);
}
