using FluentAssertions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Persistence;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class RankingServiceTests
{
    [Fact]
    public async Task Ranking_ShouldSortByTotalPointsDescending()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var kuba, out _);
        AddSettledPrediction(dbContext, marek.Id, 3, true, true);
        AddSettledPrediction(dbContext, kuba.Id, 1, false, true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var ranking = await service.GetRankingAsync();

        ranking.Select(entry => entry.UserId).First().Should().Be(marek.Id);
    }

    [Fact]
    public async Task Ranking_ShouldUseExactScoreTieBreaker()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var kuba, out _);
        AddSettledPrediction(dbContext, marek.Id, 3, true, true);
        AddSettledPrediction(dbContext, kuba.Id, 3, false, true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var ranking = await service.GetRankingAsync();

        ranking.Select(entry => entry.UserId).First().Should().Be(marek.Id);
    }

    [Fact]
    public async Task Ranking_ShouldUseCorrectOutcomeTieBreaker()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var kuba, out _);
        AddSettledPrediction(dbContext, marek.Id, 1, false, true);
        AddSettledPrediction(dbContext, marek.Id, 1, false, true);
        AddSettledPrediction(dbContext, kuba.Id, 1, false, true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var ranking = await service.GetRankingAsync();

        ranking.Select(entry => entry.UserId).First().Should().Be(marek.Id);
    }

    [Fact]
    public async Task Ranking_ShouldIncludeUserAvatarUrl()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out _, out _);
        marek.AvatarUrl = "https://cdn.example.com/marek.jpg";
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var ranking = await service.GetRankingAsync();

        ranking.Single(entry => entry.UserId == marek.Id).AvatarUrl.Should().Be("https://cdn.example.com/marek.jpg");
    }

    [Fact]
    public async Task Ranking_ShouldIgnoreResultsFromUnsettledMatches()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out _, out _);
        var settledMatch = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-2));
        var recalculationPendingMatch = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-1));
        recalculationPendingMatch.IsSettled = false;
        recalculationPendingMatch.Status = MatchStatus.Finished;
        AddSettledPrediction(dbContext, marek.Id, settledMatch.Id, 3, true, true);
        AddSettledPrediction(dbContext, marek.Id, recalculationPendingMatch.Id, 3, true, true);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var ranking = await service.GetRankingAsync();

        ranking.Single(entry => entry.UserId == marek.Id).TotalPoints.Should().Be(3);
    }

    [Fact]
    public async Task ProgressForRanking_ShouldGroupSnapshotsByPlayerWithMatchLabels()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out var kuba, out _);
        var matchOne = AddSettledMatch(dbContext, 1, "POL", "GER", DateTime.UtcNow.AddDays(-2));
        var matchTwo = AddSettledMatch(dbContext, 2, "FRA", "ESP", DateTime.UtcNow.AddDays(-1));
        AddSnapshot(dbContext, matchOne.Id, marek.Id, totalPoints: 3, position: 1);
        AddSnapshot(dbContext, matchTwo.Id, marek.Id, totalPoints: 4, position: 2);
        AddSnapshot(dbContext, matchOne.Id, kuba.Id, totalPoints: 1, position: 2);
        AddSnapshot(dbContext, matchTwo.Id, kuba.Id, totalPoints: 6, position: 1);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var progress = await service.GetProgressForRankingAsync(marek.Id);

        progress.Select(series => series.DisplayName).Should().Equal("Kuba", "Marek");
        progress.First().Points.Select(point => point.MatchLabel).Should().Equal("POL-GER", "FRA-ESP");
        progress.First().Points.Select(point => point.TotalPoints).Should().Equal(1, 6);
        progress.Single(series => series.UserId == marek.Id).IsCurrentUser.Should().BeTrue();
    }

    [Fact]
    public async Task ProgressForRanking_ShouldUseSnapshotTimeAsTieBreakerForSameKickoffMatches()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out _, out _);
        var sharedKickoff = new DateTime(2026, 6, 26, 16, 0, 0, DateTimeKind.Utc);
        var lowerNumberMatch = AddSettledMatch(dbContext, 10, "ECU", "GER", sharedKickoff);
        var higherNumberMatch = AddSettledMatch(dbContext, 20, "CUI", "CIV", sharedKickoff);
        AddSnapshot(
            dbContext,
            lowerNumberMatch.Id,
            marek.Id,
            totalPoints: 6,
            position: 1,
            createdAtUtc: sharedKickoff.AddHours(3));
        AddSnapshot(
            dbContext,
            higherNumberMatch.Id,
            marek.Id,
            totalPoints: 3,
            position: 1,
            createdAtUtc: sharedKickoff.AddHours(2));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var progress = await service.GetProgressForRankingAsync(marek.Id);

        var points = progress.Single(series => series.UserId == marek.Id).Points.ToList();
        points.Select(point => point.MatchLabel).Should().Equal("CUI-CIV", "ECU-GER");
        points.Select(point => point.TotalPoints).Should().Equal(3, 6);
    }

    [Fact]
    public async Task ProgressForRanking_ShouldIncludeActiveAdminsWithSnapshots()
    {
        using var dbContext = TestDbContextFactory.Create();
        SeedUsers(dbContext, out var marek, out _, out _);
        var admin = CreateUser("Marco", UserRole.Admin);
        var match = AddSettledMatch(dbContext, 1, "KOR", "CZE", DateTime.UtcNow.AddDays(-1));
        dbContext.Users.Add(admin);
        AddSnapshot(dbContext, match.Id, marek.Id, totalPoints: 3, position: 1);
        AddSnapshot(dbContext, match.Id, admin.Id, totalPoints: 3, position: 2);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var progress = await service.GetProgressForRankingAsync(admin.Id);

        progress.Select(series => series.DisplayName).Should().Contain("Marco");
        progress.Single(series => series.UserId == admin.Id).IsCurrentUser.Should().BeTrue();
    }

    private static RankingService CreateService(WorldCupTyperDbContext dbContext)
    {
        var builder = new LeaderboardBuilder(dbContext);
        return new RankingService(builder, dbContext);
    }

    private static void SeedUsers(WorldCupTyperDbContext dbContext, out ApplicationUser marek, out ApplicationUser kuba, out ApplicationUser bartek)
    {
        marek = CreateUser("Marek");
        kuba = CreateUser("Kuba");
        bartek = CreateUser("Bartek");

        dbContext.Users.AddRange(marek, kuba, bartek);
    }

    private static ApplicationUser CreateUser(string displayName, UserRole role = UserRole.Player)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = $"{displayName.ToLowerInvariant()}@test.local",
            DisplayName = displayName,
            PasswordHash = "hash",
            Role = role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    private static void AddSettledPrediction(WorldCupTyperDbContext dbContext, Guid userId, int points, bool exact, bool outcome)
    {
        var match = AddSettledMatch(
            dbContext,
            Random.Shared.Next(10_000, 99_999),
            "AAA",
            "BBB",
            DateTime.UtcNow.AddDays(-1));

        AddSettledPrediction(dbContext, userId, match.Id, points, exact, outcome);
    }

    private static void AddSettledPrediction(WorldCupTyperDbContext dbContext, Guid userId, Guid matchId, int points, bool exact, bool outcome)
    {
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = 1,
            PredictedAwayScore = 0,
            CreatedAtUtc = DateTime.UtcNow,
            Result = new PredictionResult
            {
                Id = Guid.NewGuid(),
                Points = points,
                IsExactScore = exact,
                IsCorrectOutcome = outcome,
                CalculatedAtUtc = DateTime.UtcNow,
            },
        };

        dbContext.Predictions.Add(prediction);
    }

    private static Match AddSettledMatch(WorldCupTyperDbContext dbContext, int matchNumber, string homeShortName, string awayShortName, DateTime kickoffTimeUtc)
    {
        var homeTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = homeShortName,
            ShortName = homeShortName,
            CountryCode = homeShortName,
        };
        var awayTeam = new Team
        {
            Id = Guid.NewGuid(),
            Name = awayShortName,
            ShortName = awayShortName,
            CountryCode = awayShortName,
        };
        var match = new Match
        {
            Id = Guid.NewGuid(),
            MatchNumber = matchNumber,
            Phase = MatchPhase.GroupStage,
            HomeTeamId = homeTeam.Id,
            AwayTeamId = awayTeam.Id,
            KickoffTimeUtc = kickoffTimeUtc,
            Status = MatchStatus.Settled,
            HomeScore90 = 1,
            AwayScore90 = 0,
            IsSettled = true,
            CreatedAtUtc = kickoffTimeUtc.AddDays(-1),
        };

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);

        return match;
    }

    private static void AddSnapshot(
        WorldCupTyperDbContext dbContext,
        Guid matchId,
        Guid userId,
        int totalPoints,
        int position,
        DateTime? createdAtUtc = null)
    {
        dbContext.LeaderboardSnapshots.Add(new LeaderboardSnapshot
        {
            Id = Guid.NewGuid(),
            MatchId = matchId,
            UserId = userId,
            TotalPoints = totalPoints,
            ExactScoreHits = totalPoints / 3,
            CorrectOutcomeHits = totalPoints,
            PredictionsCount = 1,
            Position = position,
            CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
        });
    }
}
