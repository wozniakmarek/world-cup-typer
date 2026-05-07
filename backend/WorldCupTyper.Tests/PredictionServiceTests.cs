using FluentAssertions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class PredictionServiceTests
{
    [Fact]
    public async Task CreatePrediction_AfterKickoff_ShouldThrow()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new PredictionService(dbContext, dateTimeProvider);
        var user = CreateUser();
        var match = CreateMatch(dateTimeProvider.UtcNow.AddMinutes(-1));
        dbContext.Users.Add(user);
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        var action = async () => await service.CreatePredictionAsync(user.Id, match.Id, new SavePredictionRequest(1, 0));

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Nie można zmienić typu po rozpoczęciu meczu.");
    }

    [Fact]
    public async Task UpdatePrediction_AfterKickoff_ShouldThrow()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new PredictionService(dbContext, dateTimeProvider);
        var user = CreateUser();
        var match = CreateMatch(dateTimeProvider.UtcNow.AddMinutes(-1));
        var prediction = CreatePrediction(user.Id, match.Id, 0, 0);
        dbContext.Users.Add(user);
        dbContext.Matches.Add(match);
        dbContext.Predictions.Add(prediction);
        await dbContext.SaveChangesAsync();

        var action = async () => await service.UpdatePredictionAsync(user.Id, match.Id, new SavePredictionRequest(1, 1));

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Nie można zmienić typu po rozpoczęciu meczu.");
    }

    [Fact]
    public async Task UpdatePrediction_BeforeKickoff_ShouldUpdateScores()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new PredictionService(dbContext, dateTimeProvider);
        var user = CreateUser();
        var match = CreateMatch(dateTimeProvider.UtcNow.AddHours(1));
        var prediction = CreatePrediction(user.Id, match.Id, 0, 0);
        dbContext.Users.Add(user);
        dbContext.Matches.Add(match);
        dbContext.Predictions.Add(prediction);
        await dbContext.SaveChangesAsync();

        var result = await service.UpdatePredictionAsync(user.Id, match.Id, new SavePredictionRequest(2, 1));

        result.PredictedHomeScore.Should().Be(2);
        result.PredictedAwayScore.Should().Be(1);
    }

    [Fact]
    public async Task CreatePrediction_DuplicateForSameUserAndMatch_ShouldThrow()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new PredictionService(dbContext, dateTimeProvider);
        var user = CreateUser();
        var match = CreateMatch(dateTimeProvider.UtcNow.AddHours(2));
        dbContext.Users.Add(user);
        dbContext.Matches.Add(match);
        dbContext.Predictions.Add(CreatePrediction(user.Id, match.Id, 1, 0));
        await dbContext.SaveChangesAsync();

        var action = async () => await service.CreatePredictionAsync(user.Id, match.Id, new SavePredictionRequest(2, 0));

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Użytkownik ma już zapisany typ dla tego meczu.");
    }

    private static ApplicationUser CreateUser()
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "marek@test.local",
            DisplayName = "Marek",
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    private static Match CreateMatch(DateTime kickoffTimeUtc)
    {
        return new Match
        {
            Id = Guid.NewGuid(),
            MatchNumber = Random.Shared.Next(1, 10_000),
            Phase = MatchPhase.GroupStage,
            HomeTeamId = Guid.NewGuid(),
            AwayTeamId = Guid.NewGuid(),
            KickoffTimeUtc = kickoffTimeUtc,
            Status = MatchStatus.Scheduled,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    private static Prediction CreatePrediction(Guid userId, Guid matchId, int homeScore, int awayScore)
    {
        return new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = homeScore,
            PredictedAwayScore = awayScore,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
