using FluentAssertions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Persistence;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class FinalSummaryServiceTests
{
    [Fact]
    public async Task GetFinalSummaryAsync_ShouldReturnActivePlayerPositionSeriesSortedByFinalPosition()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out var inactive);
        inactive.IsActive = false;
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-2));
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-1));
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 2, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 9, exact: 3, outcome: 3, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchOne.Id, tomek.Id, totalPoints: 6, exact: 2, outcome: 2, predictions: 1, position: 1, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, tomek.Id, totalPoints: 7, exact: 2, outcome: 3, predictions: 2, position: 2, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, inactive.Id, totalPoints: 99, exact: 33, outcome: 33, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync(marek.Id);

        summary.Stats.SettledMatchesCount.Should().Be(2);
        summary.Stats.ActivePlayersCount.Should().Be(2);
        summary.Stats.FinalLeaderUserId.Should().Be(marek.Id);
        summary.PositionSeries.Select(series => series.DisplayName).Should().Equal("Marek", "Tomek");
        summary.PositionSeries.Single(series => series.UserId == marek.Id).IsCurrentUser.Should().BeTrue();
        summary.PositionSeries.Single(series => series.UserId == marek.Id).Points.Select(point => point.Position).Should().Equal(2, 1);
        summary.FinalTop.Select(entry => entry.DisplayName).Should().Equal("Marek", "Tomek");
    }

    [Fact]
    public async Task GetFinalSummaryAsync_ShouldReturnGlobalFactsFromSnapshotsAndPredictions()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var tomek, out _);
        var matchOne = AddSettledMatch(dbContext, 1, "MEX", "RSA", DateTime.UtcNow.AddDays(-2), homeScore: 2, awayScore: 0);
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-1), homeScore: 1, awayScore: 1);
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, exact: 1, outcome: 1, predictions: 1, position: 8, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 10, exact: 2, outcome: 4, predictions: 2, position: 1, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchOne.Id, tomek.Id, totalPoints: 6, exact: 2, outcome: 2, predictions: 1, position: 1, createdAtUtc: matchOne.KickoffTimeUtc.AddHours(2));
        AddSnapshot(dbContext, matchTwo.Id, tomek.Id, totalPoints: 7, exact: 2, outcome: 3, predictions: 2, position: 4, createdAtUtc: matchTwo.KickoffTimeUtc.AddHours(2));
        AddPrediction(dbContext, marek.Id, matchOne.Id, 2, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchOne.Id, 2, 0, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, marek.Id, matchTwo.Id, 1, 1, points: 3, exact: true, outcome: true);
        AddPrediction(dbContext, tomek.Id, matchTwo.Id, 0, 0, points: 1, exact: false, outcome: true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var summary = await service.GetFinalSummaryAsync();

        summary.GlobalFacts.Should().Contain(fact => fact.Id == "biggest-climb" && fact.RelatedUserIds.Contains(marek.Id));
        summary.GlobalFacts.Should().Contain(fact => fact.Id == "biggest-drop" && fact.RelatedUserIds.Contains(tomek.Id));
        summary.GlobalFacts.Should().Contain(fact => fact.Id == "most-exact-match" && fact.RelatedMatchIds.Contains(matchOne.Id));
        summary.GlobalFacts.Should().Contain(fact => fact.Id == "draw-specialist");
        summary.GlobalFacts.Count.Should().BeGreaterThanOrEqualTo(4);
    }

    private static FinalSummaryService CreateService(WorldCupTyperDbContext dbContext)
    {
        return new FinalSummaryService(dbContext);
    }

    private static void SeedUsers(WorldCupTyperDbContext dbContext, out ApplicationUser marek, out ApplicationUser tomek, out ApplicationUser inactive)
    {
        marek = CreateUser("Marek");
        tomek = CreateUser("Tomek");
        inactive = CreateUser("Inactive");
        dbContext.Users.AddRange(marek, tomek, inactive);
    }

    private static ApplicationUser CreateUser(string displayName)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{displayName.ToLowerInvariant()}@test.local",
            DisplayName = displayName,
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    private static Match AddSettledMatch(WorldCupTyperDbContext dbContext, int matchNumber, string homeShortName, string awayShortName, DateTime kickoffTimeUtc, int homeScore = 1, int awayScore = 0)
    {
        var homeTeam = new Team { Id = Guid.NewGuid(), Name = homeShortName, ShortName = homeShortName, CountryCode = homeShortName };
        var awayTeam = new Team { Id = Guid.NewGuid(), Name = awayShortName, ShortName = awayShortName, CountryCode = awayShortName };
        var match = new Match
        {
            Id = Guid.NewGuid(),
            MatchNumber = matchNumber,
            Phase = MatchPhase.GroupStage,
            HomeTeamId = homeTeam.Id,
            HomeTeam = homeTeam,
            AwayTeamId = awayTeam.Id,
            AwayTeam = awayTeam,
            KickoffTimeUtc = kickoffTimeUtc,
            Status = MatchStatus.Settled,
            HomeScore90 = homeScore,
            AwayScore90 = awayScore,
            IsSettled = true,
            CreatedAtUtc = kickoffTimeUtc.AddDays(-1),
        };

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        return match;
    }

    private static void AddSnapshot(WorldCupTyperDbContext dbContext, Guid matchId, Guid userId, int totalPoints, int exact, int outcome, int predictions, int position, DateTime createdAtUtc)
    {
        dbContext.LeaderboardSnapshots.Add(new LeaderboardSnapshot
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            UserId = userId,
            TotalPoints = totalPoints,
            ExactScoreHits = exact,
            CorrectOutcomeHits = outcome,
            PredictionsCount = predictions,
            Position = position,
            CreatedAtUtc = createdAtUtc,
        });
    }

    private static void AddPrediction(WorldCupTyperDbContext dbContext, Guid userId, Guid matchId, int predictedHome, int predictedAway, int points, bool exact, bool outcome)
    {
        dbContext.Predictions.Add(new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = predictedHome,
            PredictedAwayScore = predictedAway,
            CreatedAtUtc = DateTime.UtcNow,
            Result = new PredictionResult
            {
                Id = Guid.NewGuid(),
                Points = points,
                IsExactScore = exact,
                IsCorrectOutcome = outcome,
                CalculatedAtUtc = DateTime.UtcNow,
            },
        });
    }
}
