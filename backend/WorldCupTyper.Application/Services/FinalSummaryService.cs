using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Services;

public sealed class FinalSummaryService : IFinalSummaryService
{
    private const string FinalHomeTeamCode = "ARG";
    private const string FinalAwayTeamCode = "ESP";
    private const string ExpectedFinalMatchLabel = "ARG-ESP";

    private readonly IAppDbContext _dbContext;

    public FinalSummaryService(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FinalSummaryAvailabilityDto> GetFinalSummaryAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var matches = await _dbContext.Matches
            .AsNoTracking()
            .Include(match => match.HomeTeam)
            .Include(match => match.AwayTeam)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);

        var totalMatchesCount = matches.Count;
        var settledMatchesCount = matches.Count(match => match.IsSettled);
        var requiredSettledMatchesCount = totalMatchesCount;
        var finalMatch = matches
            .Where(IsExpectedFinalMatch)
            .OrderByDescending(match => match.MatchNumber)
            .FirstOrDefault();
        var finalMatchLabel = finalMatch is null ? ExpectedFinalMatchLabel : BuildMatchLabel(finalMatch);

        if (finalMatch is null)
        {
            return BuildAvailability(false, "final-match-missing");
        }

        if (!finalMatch.IsSettled)
        {
            return BuildAvailability(false, "final-match-not-settled");
        }

        if (settledMatchesCount < requiredSettledMatchesCount)
        {
            return BuildAvailability(false, "matches-still-open");
        }

        var finalPredictions = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => prediction.MatchId == finalMatch.Id)
            .Select(prediction => new
            {
                HasResult = prediction.Result != null,
            })
            .ToListAsync(cancellationToken);
        var finalResultsCalculated = finalPredictions.All(prediction => prediction.HasResult);

        if (!finalResultsCalculated)
        {
            return BuildAvailability(false, "final-results-not-calculated");
        }

        var finalRankingSnapshotted = await _dbContext.LeaderboardSnapshots
            .AsNoTracking()
            .AnyAsync(snapshot => snapshot.MatchId == finalMatch.Id, cancellationToken);

        if (!finalRankingSnapshotted)
        {
            return BuildAvailability(false, "final-ranking-not-snapshotted");
        }

        return BuildAvailability(true, "ready");

        FinalSummaryAvailabilityDto BuildAvailability(bool isReady, string reason)
        {
            return new FinalSummaryAvailabilityDto(
                isReady,
                reason,
                settledMatchesCount,
                requiredSettledMatchesCount,
                totalMatchesCount,
                finalMatchLabel);
        }
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
            BuildGlobalFacts(seriesData, predictionRows, settledMatches, activeUsers));
    }

    public async Task<PersonalFinalSummaryResponseDto> GetPersonalFinalSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var summary = await GetFinalSummaryAsync(userId, cancellationToken);
        var finalEntry = summary.FinalTop.FirstOrDefault(entry => entry.UserId == userId);
        if (finalEntry is not null)
        {
            var settledMatches = await _dbContext.Matches
                .AsNoTracking()
                .Where(match => match.IsSettled)
                .Include(match => match.HomeTeam)
                .Include(match => match.AwayTeam)
                .OrderBy(match => match.KickoffTimeUtc)
                .ThenBy(match => match.MatchNumber)
                .ToListAsync(cancellationToken);
            var settledMatchIds = settledMatches.Select(match => match.Id).ToList();
            var predictionRows = await LoadPredictionRowsAsync(new[] { userId }, settledMatchIds, cancellationToken);
            var activeUserIds = summary.FinalTop.Select(entry => entry.UserId).ToList();
            var allPredictionRows = await LoadPredictionRowsAsync(activeUserIds, settledMatchIds, cancellationToken);
            var personalFacts = BuildPersonalFacts(
                finalEntry,
                summary.PositionSeries.FirstOrDefault(series => series.UserId == userId),
                predictionRows,
                allPredictionRows,
                settledMatches);
            var highlightedMatchIds = personalFacts
                .SelectMany(fact => fact.RelatedMatchIds)
                .Distinct()
                .ToList();

            return new PersonalFinalSummaryResponseDto(
                finalEntry.UserId,
                finalEntry.DisplayName,
                finalEntry.AvatarUrl,
                finalEntry.FinalPosition,
                finalEntry.TotalPoints,
                finalEntry.ExactScoreHits,
                finalEntry.CorrectOutcomeHits,
                finalEntry.PredictionsCount,
                personalFacts,
                highlightedMatchIds);
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
        IReadOnlyCollection<Match> settledMatches,
        IReadOnlyCollection<ApplicationUser> activeUsers)
    {
        var activeUserDisplayNames = activeUsers.ToDictionary(user => user.Id, user => user.DisplayName);
        var facts = new List<FinalSummaryFactDto>();
        AddBiggestTrapFact(facts, predictionRows, settledMatches);
        AddNoExactPerfectDirectionFact(facts, predictionRows, settledMatches);
        AddSoloExactKingFact(facts, predictionRows, settledMatches, activeUserDisplayNames);
        AddLongestExactStreakFact(facts, predictionRows, settledMatches, activeUserDisplayNames);
        AddBiggestClimbFact(facts, seriesData);
        AddBiggestDropFact(facts, seriesData);
        AddMostExactMatchFact(facts, predictionRows, settledMatches);
        AddDrawSpecialistFact(facts, predictionRows, settledMatches, activeUserDisplayNames);
        AddStrongestFinishFact(facts, seriesData);
        AddScorelineMagnetFact(facts, predictionRows);
        AddMostConsistentFact(facts, predictionRows, activeUserDisplayNames);
        AddOneGoalAwayFact(facts, predictionRows, settledMatches, activeUserDisplayNames);
        return facts;
    }

    private static List<FinalSummaryFactDto> BuildPersonalFacts(
        FinalRankingEntryDto finalEntry,
        FinalRankingPositionSeriesDto? positionSeries,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<PredictionSummaryRow> allPredictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var facts = new List<FinalSummaryFactDto>();

        facts.Add(new FinalSummaryFactDto(
            "personal-final-rank",
            "Finalne miejsce",
            $"Miejsce #{finalEntry.FinalPosition}",
            $"{finalEntry.DisplayName} kończy z {finalEntry.TotalPoints} pkt, {finalEntry.ExactScoreHits} dokładnymi wynikami i {finalEntry.CorrectOutcomeHits} trafionymi rozstrzygnięciami.",
            new[] { finalEntry.UserId },
            Array.Empty<Guid>()));

        AddPersonalSoloExactFact(facts, finalEntry, predictionRows, allPredictionRows, settledMatches);
        AddPersonalExactStreakFact(facts, finalEntry, predictionRows, settledMatches);

        if (positionSeries is not null && positionSeries.Points.Count > 1)
        {
            var firstPosition = positionSeries.Points.First().Position;
            var climb = firstPosition - positionSeries.FinalPosition;
            if (climb > 0)
            {
                facts.Add(new FinalSummaryFactDto(
                    "personal-biggest-climb",
                    "Największy awans",
                    $"Awans o {climb} miejsc",
                    $"{finalEntry.DisplayName} przesunął się z miejsca {firstPosition} na {positionSeries.FinalPosition}.",
                    new[] { finalEntry.UserId },
                    positionSeries.Points.Select(point => point.MatchId).TakeLast(1).ToList()));
            }

            var drop = positionSeries.FinalPosition - firstPosition;
            if (drop > 0)
            {
                facts.Add(new FinalSummaryFactDto(
                    "personal-biggest-drop",
                    "Zmiana pozycji",
                    $"Spadek o {drop} miejsc",
                    $"{finalEntry.DisplayName} zaczynał na miejscu {firstPosition}, a zakończył na {positionSeries.FinalPosition}.",
                    new[] { finalEntry.UserId },
                    positionSeries.Points.Select(point => point.MatchId).TakeLast(1).ToList()));
            }
        }

        var bestPoints = predictionRows.Count == 0
            ? 0
            : predictionRows.Max(row => row.Points);
        if (bestPoints > 0)
        {
            var bestMatchIds = predictionRows
                .Where(row => row.Points == bestPoints)
                .Select(row => row.MatchId)
                .Where(matchById.ContainsKey)
                .OrderBy(matchId => matchById[matchId].MatchNumber)
                .ToList();

            facts.Add(new FinalSummaryFactDto(
                "personal-best-match",
                "Najlepszy mecz",
                bestPoints == 3 ? $"Dokładne trafienia: {bestMatchIds.Count}" : $"Najlepszy typ: {bestPoints} pkt",
                bestPoints == 3
                    ? $"Typy za 3 pkt w meczach: {BuildMatchList(bestMatchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))}."
                    : $"Najlepszy typ w meczach: {BuildMatchList(bestMatchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))}.",
                new[] { finalEntry.UserId },
                bestMatchIds));
        }

        var favoriteScoreline = predictionRows
            .GroupBy(row => new { row.PredictedHomeScore, row.PredictedAwayScore })
            .Select(group => new
            {
                group.Key.PredictedHomeScore,
                group.Key.PredictedAwayScore,
                Count = group.Count(),
                MatchIds = group.Select(row => row.MatchId).Where(matchById.ContainsKey).ToList(),
            })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.PredictedHomeScore)
            .ThenBy(entry => entry.PredictedAwayScore)
            .FirstOrDefault();

        if (favoriteScoreline is not null)
        {
            facts.Add(new FinalSummaryFactDto(
                "personal-favorite-scoreline",
                "Ulubiony wynik",
                $"{favoriteScoreline.PredictedHomeScore}:{favoriteScoreline.PredictedAwayScore} typowane {favoriteScoreline.Count} razy",
                $"{finalEntry.DisplayName} najczęściej wybierał wynik {favoriteScoreline.PredictedHomeScore}:{favoriteScoreline.PredictedAwayScore}.",
                new[] { finalEntry.UserId },
                favoriteScoreline.MatchIds));
        }

        var nonExactRows = predictionRows.Where(row => !row.IsExactScore).ToList();
        if (nonExactRows.Count > 0)
        {
            facts.Add(new FinalSummaryFactDto(
                "personal-non-exact-count",
                "Typy blisko celu",
                $"Niedokładne typy: {nonExactRows.Count}",
                $"{finalEntry.DisplayName} miał {nonExactRows.Count} rozliczonych typów bez dokładnego wyniku.",
                new[] { finalEntry.UserId },
                nonExactRows
                    .Select(row => row.MatchId)
                    .Where(matchById.ContainsKey)
                    .Distinct()
                    .OrderBy(matchId => matchById[matchId].MatchNumber)
                    .ToList()));
        }

        return facts
            .GroupBy(fact => fact.Id)
            .Select(group => group.First())
            .Take(6)
            .ToList();
    }

    private static void AddPersonalSoloExactFact(
        List<FinalSummaryFactDto> facts,
        FinalRankingEntryDto finalEntry,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<PredictionSummaryRow> allPredictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var exactCountsByMatch = allPredictionRows
            .GroupBy(row => row.MatchId)
            .ToDictionary(group => group.Key, group => group.Count(row => row.IsExactScore));
        var soloMatchIds = predictionRows
            .Where(row => row.IsExactScore && exactCountsByMatch.TryGetValue(row.MatchId, out var exactCount) && exactCount == 1)
            .Select(row => row.MatchId)
            .Where(matchById.ContainsKey)
            .Distinct()
            .OrderBy(matchId => matchById[matchId].MatchNumber)
            .ToList();

        if (soloMatchIds.Count == 0)
        {
            return;
        }

        facts.Add(new FinalSummaryFactDto(
            "personal-solo-exacts",
            "Samotne trafienia",
            $"Samotne trafienia: {soloMatchIds.Count}",
            $"W tych meczach tylko {finalEntry.DisplayName} trafił dokładnie: {JoinNames(soloMatchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))}.",
            new[] { finalEntry.UserId },
            soloMatchIds));
    }

    private static void AddPersonalExactStreakFact(
        List<FinalSummaryFactDto> facts,
        FinalRankingEntryDto finalEntry,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var bestStreak = BuildExactStreaks(predictionRows, settledMatches)
            .Where(streak => streak.UserId == finalEntry.UserId)
            .OrderByDescending(streak => streak.Length)
            .ThenBy(streak => matchById[streak.MatchIds.First()].MatchNumber)
            .FirstOrDefault();

        if (bestStreak is null)
        {
            return;
        }

        facts.Add(new FinalSummaryFactDto(
            "personal-exact-streak",
            "Seria dokładnych",
            $"Seria dokładnych: {bestStreak.Length}",
            $"{finalEntry.DisplayName}: dokładne wyniki z rzędu w meczach {JoinNames(bestStreak.MatchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))}.",
            new[] { finalEntry.UserId },
            bestStreak.MatchIds));
    }

    private static void AddBiggestTrapFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var traps = predictionRows
            .GroupBy(row => row.MatchId)
            .Select(group => new
            {
                MatchId = group.Key,
                Predictions = group.Count(),
                WrongOutcomes = group.Count(row => !row.IsCorrectOutcome),
                OutcomeHits = group.Count(row => row.IsCorrectOutcome),
            })
            .Where(entry => entry.WrongOutcomes > 0 && matchById.ContainsKey(entry.MatchId))
            .OrderByDescending(entry => entry.OutcomeHits == 0)
            .ThenByDescending(entry => entry.WrongOutcomes)
            .ThenByDescending(entry => entry.Predictions)
            .ThenBy(entry => matchById[entry.MatchId].MatchNumber)
            .FirstOrDefault();

        if (traps is null)
        {
            return;
        }

        var match = matchById[traps.MatchId];
        facts.Add(new FinalSummaryFactDto(
            "biggest-trap",
            "Mecz-pułapka",
            $"{BuildMatchLabel(match)} {BuildScoreLabel(match)}: {traps.WrongOutcomes} nietrafionych kierunków",
            traps.OutcomeHits == 0
                ? "Tu wszyscy poszli w złą stronę. Ani jeden typ nie złapał rozstrzygnięcia."
                : $"Tu {traps.WrongOutcomes} typów poszło w złą stronę, mimo że {traps.OutcomeHits} złapało rozstrzygnięcie.",
            Array.Empty<Guid>(),
            new[] { traps.MatchId }));
    }

    private static void AddNoExactPerfectDirectionFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var candidate = predictionRows
            .GroupBy(row => row.MatchId)
            .Select(group => new
            {
                MatchId = group.Key,
                Predictions = group.Count(),
                ExactHits = group.Count(row => row.IsExactScore),
                OutcomeHits = group.Count(row => row.IsCorrectOutcome),
            })
            .Where(entry => entry.ExactHits == 0 && entry.OutcomeHits > 0 && matchById.ContainsKey(entry.MatchId))
            .OrderByDescending(entry => entry.OutcomeHits == entry.Predictions)
            .ThenByDescending(entry => entry.OutcomeHits)
            .ThenByDescending(entry => entry.Predictions)
            .ThenBy(entry => matchById[entry.MatchId].MatchNumber)
            .FirstOrDefault();

        if (candidate is null)
        {
            return;
        }

        var match = matchById[candidate.MatchId];
        facts.Add(new FinalSummaryFactDto(
            "no-exact-perfect-direction",
            "Kierunek tak, wynik nie",
            $"{BuildMatchLabel(match)} {BuildScoreLabel(match)}: {candidate.OutcomeHits} kierunków, 0 dokładnych",
            "Grupa dobrze przeczuła kierunek meczu, ale nikt nie przewidział rozmiaru wyniku.",
            Array.Empty<Guid>(),
            new[] { candidate.MatchId }));
    }

    private static void AddSoloExactKingFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches,
        IReadOnlyDictionary<Guid, string> activeUserDisplayNames)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var exactCountsByMatch = predictionRows
            .GroupBy(row => row.MatchId)
            .ToDictionary(group => group.Key, group => group.Count(row => row.IsExactScore));
        var soloRows = predictionRows
            .Where(row => row.IsExactScore && exactCountsByMatch.TryGetValue(row.MatchId, out var exactCount) && exactCount == 1)
            .Where(row => matchById.ContainsKey(row.MatchId))
            .ToList();

        var leaders = soloRows
            .GroupBy(row => row.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Count = group.Count(),
                MatchIds = group.Select(row => row.MatchId).Distinct().OrderBy(matchId => matchById[matchId].MatchNumber).ToList(),
            })
            .Where(entry => entry.Count > 0)
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => activeUserDisplayNames.TryGetValue(entry.UserId, out var displayName) ? displayName : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (leaders.Count == 0)
        {
            return;
        }

        var maxCount = leaders.First().Count;
        var winners = leaders.Where(entry => entry.Count == maxCount).ToList();
        var matchIds = winners
            .SelectMany(winner => winner.MatchIds)
            .Distinct()
            .OrderBy(matchId => matchById[matchId].MatchNumber)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "solo-exact-king",
            "Samotne trafienia",
            $"{JoinNames(winners.Select(winner => activeUserDisplayNames.TryGetValue(winner.UserId, out var displayName) ? displayName : "Gracz"))}: {maxCount} solo",
            $"{JoinNames(winners.Select(winner => activeUserDisplayNames.TryGetValue(winner.UserId, out var displayName) ? displayName : "Gracz"))}: samotnie trafione dokładne wyniki w meczach {BuildMatchList(matchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))}.",
            winners.Select(winner => winner.UserId).ToList(),
            matchIds));
    }

    private static void AddLongestExactStreakFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches,
        IReadOnlyDictionary<Guid, string> activeUserDisplayNames)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var streaks = BuildExactStreaks(predictionRows, settledMatches)
            .OrderByDescending(streak => streak.Length)
            .ThenBy(streak => matchById[streak.MatchIds.First()].MatchNumber)
            .ThenBy(streak => activeUserDisplayNames.TryGetValue(streak.UserId, out var displayName) ? displayName : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (streaks.Count == 0)
        {
            return;
        }

        var bestLength = streaks.First().Length;
        var winners = streaks.Where(streak => streak.Length == bestLength).ToList();
        var matchIds = winners
            .SelectMany(winner => winner.MatchIds)
            .Distinct()
            .OrderBy(matchId => matchById[matchId].MatchNumber)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "longest-exact-streak",
            "Seria dokładnych",
            $"{JoinNames(winners.Select(winner => activeUserDisplayNames.TryGetValue(winner.UserId, out var displayName) ? displayName : "Gracz"))}: {bestLength} z rzędu",
            $"Najdłuższa seria: dokładne wyniki z rzędu w meczach {JoinNames(matchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))}.",
            winners.Select(winner => winner.UserId).ToList(),
            matchIds));
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
            "Największy awans",
            $"Awans o {biggestClimb} miejsc",
            $"{JoinNames(winners.Select(winner => winner.DisplayName))}: najmocniejsza poprawa pozycji od pierwszego do finałowego pomiaru.",
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
            "Największy spadek",
            $"Spadek o {biggestDrop} miejsc",
            $"{JoinNames(players.Select(player => player.DisplayName))}: największa strata miejsc od pierwszego do finałowego pomiaru.",
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
            "Najwięcej dokładnych wyników",
            $"Najwięcej idealnych typów: {maxExactHits}",
            matchIds.Count == 1
                ? $"{BuildMatchLabel(matchById[matchIds[0]])}: {maxExactHits} dokładnych typów."
                : $"{BuildMatchList(matchIds.Select(matchId => BuildMatchLabel(matchById[matchId])))}: po {maxExactHits} dokładnych typów.",
            Array.Empty<Guid>(),
            matchIds));
    }

    private static void AddDrawSpecialistFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches,
        IReadOnlyDictionary<Guid, string> activeUserDisplayNames)
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

        var maxHits = drawHitsByUser.Max(entry => entry.Hits);
        var userIds = drawHitsByUser
            .Where(entry => entry.Hits == maxHits)
            .Select(entry => entry.UserId)
            .OrderBy(userId => activeUserDisplayNames.TryGetValue(userId, out var displayName) ? displayName : string.Empty, StringComparer.OrdinalIgnoreCase)
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
            "Specjalista od remisów",
            $"Trafione remisy: {maxHits}",
            userIds.Count == 1
                ? $"{JoinNames(userIds.Select(userId => activeUserDisplayNames.TryGetValue(userId, out var displayName) ? displayName : "Gracz"))} najlepiej czytał remisy."
                : $"{JoinNames(userIds.Select(userId => activeUserDisplayNames.TryGetValue(userId, out var displayName) ? displayName : "Gracz"))} najlepiej czytali remisy.",
            userIds,
            relatedMatchIds));
    }

    private static void AddStrongestFinishFact(List<FinalSummaryFactDto> facts, IReadOnlyCollection<FinalPositionSeriesData> seriesData)
    {
        var finishes = seriesData
            .Where(data => data.Series.Points.Count > 1)
            .Select(data =>
            {
                var points = data.Series.Points.ToList();
                var midpointIndex = Math.Max(0, (points.Count / 2) - 1);
                var secondHalfGain = points.Last().TotalPoints - points[midpointIndex].TotalPoints;

                return new
                {
                    data.Series.UserId,
                    data.Series.DisplayName,
                    Gain = secondHalfGain,
                };
            })
            .Where(entry => entry.Gain > 0)
            .ToList();

        if (finishes.Count == 0)
        {
            return;
        }

        var maxGain = finishes.Max(entry => entry.Gain);
        var winners = finishes
            .Where(entry => entry.Gain == maxGain)
            .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "strongest-finish",
            "Najmocniejszy finisz",
            $"Punkty w drugiej połowie: +{maxGain}",
            $"{JoinNames(winners.Select(winner => winner.DisplayName))}: najwięcej punktów w drugiej połowie turnieju.",
            winners.Select(winner => winner.UserId).ToList(),
            Array.Empty<Guid>()));
    }

    private static void AddScorelineMagnetFact(List<FinalSummaryFactDto> facts, IReadOnlyCollection<PredictionSummaryRow> predictionRows)
    {
        var scorelines = predictionRows
            .GroupBy(row => new { row.PredictedHomeScore, row.PredictedAwayScore })
            .Select(group => new
            {
                group.Key.PredictedHomeScore,
                group.Key.PredictedAwayScore,
                Count = group.Count(),
                UserIds = group.Select(row => row.UserId).Distinct().ToList(),
                MatchIds = group.Select(row => row.MatchId).Distinct().ToList(),
            })
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.PredictedHomeScore)
            .ThenBy(entry => entry.PredictedAwayScore)
            .FirstOrDefault();

        if (scorelines is null)
        {
            return;
        }

        facts.Add(new FinalSummaryFactDto(
            "scoreline-magnet",
            "Magnes na wynik",
            $"{scorelines.PredictedHomeScore}:{scorelines.PredictedAwayScore} typowane {scorelines.Count} razy",
            $"Najczęściej wybieranym wynikiem było {scorelines.PredictedHomeScore}:{scorelines.PredictedAwayScore}.",
            scorelines.UserIds,
            scorelines.MatchIds));
    }

    private static void AddMostConsistentFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyDictionary<Guid, string> activeUserDisplayNames)
    {
        var predictionCounts = predictionRows
            .GroupBy(row => row.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Count = group.Count(),
            })
            .Where(entry => entry.Count > 0)
            .ToList();

        if (predictionCounts.Count == 0)
        {
            return;
        }

        var maxCount = predictionCounts.Max(entry => entry.Count);
        var userIds = predictionCounts
            .Where(entry => entry.Count == maxCount)
            .Select(entry => entry.UserId)
            .OrderBy(userId => activeUserDisplayNames.TryGetValue(userId, out var displayName) ? displayName : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "most-consistent",
            "Najbardziej regularni",
            $"Rozliczone typy: {maxCount}",
            $"{JoinNames(userIds.Select(userId => activeUserDisplayNames.TryGetValue(userId, out var displayName) ? displayName : "Gracz"))}: najwięcej rozliczonych typów.",
            userIds,
            Array.Empty<Guid>()));
    }

    private static void AddOneGoalAwayFact(
        List<FinalSummaryFactDto> facts,
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches,
        IReadOnlyDictionary<Guid, string> activeUserDisplayNames)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var nearMisses = predictionRows
            .Where(row => !row.IsExactScore)
            .GroupBy(row => row.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Count = group.Count(),
                MatchIds = group.Select(row => row.MatchId).Where(matchById.ContainsKey).Distinct().ToList(),
            })
            .Where(entry => entry.Count > 0)
            .ToList();

        if (nearMisses.Count == 0)
        {
            return;
        }

        var maxCount = nearMisses.Max(entry => entry.Count);
        var winners = nearMisses
            .Where(entry => entry.Count == maxCount)
            .OrderBy(entry => activeUserDisplayNames.TryGetValue(entry.UserId, out var displayName) ? displayName : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        facts.Add(new FinalSummaryFactDto(
            "one-goal-away",
            "Najwięcej niedokładnych typów",
            $"Niedokładne typy: {maxCount}",
            $"{JoinNames(winners.Select(winner => activeUserDisplayNames.TryGetValue(winner.UserId, out var displayName) ? displayName : "Gracz"))}: najwięcej rozliczonych typów bez dokładnego wyniku.",
            winners.Select(winner => winner.UserId).ToList(),
            winners.SelectMany(winner => winner.MatchIds).Distinct().OrderBy(matchId => matchById[matchId].MatchNumber).ToList()));
    }

    private static List<ExactStreak> BuildExactStreaks(
        IReadOnlyCollection<PredictionSummaryRow> predictionRows,
        IReadOnlyCollection<Match> settledMatches)
    {
        var matchById = settledMatches.ToDictionary(match => match.Id);
        var streaks = new List<ExactStreak>();

        foreach (var userRows in predictionRows.GroupBy(row => row.UserId))
        {
            var currentMatchIds = new List<Guid>();
            var orderedRows = userRows
                .Where(row => matchById.ContainsKey(row.MatchId))
                .OrderBy(row => matchById[row.MatchId].KickoffTimeUtc)
                .ThenBy(row => matchById[row.MatchId].MatchNumber)
                .ToList();

            foreach (var row in orderedRows)
            {
                if (row.IsExactScore)
                {
                    currentMatchIds.Add(row.MatchId);
                    continue;
                }

                AddStreakIfLongEnough(streaks, userRows.Key, currentMatchIds);
                currentMatchIds = new List<Guid>();
            }

            AddStreakIfLongEnough(streaks, userRows.Key, currentMatchIds);
        }

        return streaks;
    }

    private static void AddStreakIfLongEnough(List<ExactStreak> streaks, Guid userId, IReadOnlyList<Guid> matchIds)
    {
        if (matchIds.Count < 2)
        {
            return;
        }

        streaks.Add(new ExactStreak(userId, matchIds.ToList()));
    }

    private static string BuildMatchLabel(Match match)
    {
        var home = match.HomeTeam.ShortName.Trim();
        var away = match.AwayTeam.ShortName.Trim();

        return string.IsNullOrWhiteSpace(home) || string.IsNullOrWhiteSpace(away)
            ? $"M{match.MatchNumber}"
            : $"{home}-{away}";
    }

    private static bool IsExpectedFinalMatch(Match match)
    {
        return HasTeamCode(match, FinalHomeTeamCode) && HasTeamCode(match, FinalAwayTeamCode);
    }

    private static bool HasTeamCode(Match match, string teamCode)
    {
        return IsTeamCode(match.HomeTeam, teamCode) || IsTeamCode(match.AwayTeam, teamCode);
    }

    private static bool IsTeamCode(Team team, string teamCode)
    {
        return string.Equals(team.ShortName, teamCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(team.CountryCode, teamCode, StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildScoreLabel(Match match)
    {
        return match.HomeScore90.HasValue && match.AwayScore90.HasValue
            ? $"{match.HomeScore90.Value}:{match.AwayScore90.Value}"
            : "?:?";
    }

    private static string JoinNames(IEnumerable<string> names)
    {
        return string.Join(", ", names);
    }

    private static string BuildMatchList(IEnumerable<string> labels, int visibleCount = 5)
    {
        var matchLabels = labels.ToList();
        if (matchLabels.Count <= visibleCount)
        {
            return JoinNames(matchLabels);
        }

        return $"{JoinNames(matchLabels.Take(visibleCount))} i jeszcze {matchLabels.Count - visibleCount}";
    }

    private sealed record FinalPositionSeriesData(FinalRankingPositionSeriesDto Series, LeaderboardSnapshot FinalSnapshot);

    private sealed record PlayerStats(int TotalPoints, int ExactScoreHits, int CorrectOutcomeHits, int PredictionsCount);

    private sealed record ExactStreak(Guid UserId, IReadOnlyList<Guid> MatchIds)
    {
        public int Length => MatchIds.Count;
    }

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
