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

    private static void AddSettledPrediction(WorldCupTyperDbContext dbContext, Guid userId, int points, bool exact, bool outcome)
    {
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = Guid.NewGuid(),
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
}
