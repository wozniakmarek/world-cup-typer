using FluentAssertions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class MatchServiceTests
{
    [Fact]
    public async Task CreateMatch_WithBlankExternalId_ShouldPersistNull()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new MatchService(dbContext, dateTimeProvider);
        var homeTeam = CreateTeam("Polska", "POL");
        var awayTeam = CreateTeam("Niemcy", "GER");

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        await dbContext.SaveChangesAsync();

        await service.CreateMatchAsync(new UpsertMatchRequest(
            "   ",
            9001,
            MatchPhase.GroupStage,
            "A",
            homeTeam.Id,
            awayTeam.Id,
            null,
            null,
            dateTimeProvider.UtcNow.AddDays(1),
            "Warszawa"));

        var savedMatch = dbContext.Matches.Single(candidate => candidate.MatchNumber == 9001);
        savedMatch.ExternalId.Should().BeNull();
    }

    [Fact]
    public async Task SetResult_WithOnlyOneFinalScore_ShouldThrow()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new MatchService(dbContext, dateTimeProvider);
        var homeTeam = CreateTeam("Polska", "POL");
        var awayTeam = CreateTeam("Niemcy", "GER");
        var match = CreateMatch(homeTeam.Id, awayTeam.Id, dateTimeProvider.UtcNow.AddHours(-2));

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        var action = async () => await service.SetResultAsync(
            match.Id,
            new SetMatchResultRequest(1, 0, 2, null, null));

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Wynik końcowy trzeba podać dla obu drużyn albo zostawić oba pola puste.");
    }

    [Fact]
    public async Task SetResult_WithFullFinalScore_ShouldUseFinalScoreForWinner()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new MatchService(dbContext, dateTimeProvider);
        var homeTeam = CreateTeam("Francja", "FRA");
        var awayTeam = CreateTeam("Hiszpania", "ESP");
        var match = CreateMatch(homeTeam.Id, awayTeam.Id, dateTimeProvider.UtcNow.AddHours(-2));

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        var result = await service.SetResultAsync(
            match.Id,
            new SetMatchResultRequest(1, 1, 2, 1, null));

        result.HomeScore90.Should().Be(1);
        result.AwayScore90.Should().Be(1);
        result.Status.Should().Be(MatchStatus.Finished);

        var savedMatch = dbContext.Matches.Single(candidate => candidate.Id == match.Id);
        savedMatch.HomeScoreFinal.Should().Be(2);
        savedMatch.AwayScoreFinal.Should().Be(1);
        savedMatch.WinnerTeamId.Should().Be(homeTeam.Id);
    }

    [Fact]
    public async Task GetMatchesAsync_WithUnresolvedKnockoutMatch_ShouldHideItFromPlayers()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = new MatchService(dbContext, dateTimeProvider);
        var knownTeam = CreateTeam("Mexico", "MEX");
        var placeholderTeam = CreateTeam("Unknown team", "TBA");
        var match = CreateMatch(knownTeam.Id, placeholderTeam.Id, dateTimeProvider.UtcNow.AddDays(30));
        match.MatchNumber = 537417;
        match.Phase = MatchPhase.RoundOf32;

        dbContext.Teams.AddRange(knownTeam, placeholderTeam);
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        var result = await service.GetMatchesAsync(Guid.NewGuid());

        result.Should().BeEmpty();
    }

    private static Team CreateTeam(string name, string shortName)
    {
        return new Team
        {
            Id = Guid.NewGuid(),
            Name = name,
            ShortName = shortName,
            CountryCode = shortName,
        };
    }

    private static Match CreateMatch(Guid homeTeamId, Guid awayTeamId, DateTime kickoffTimeUtc)
    {
        return new Match
        {
            Id = Guid.NewGuid(),
            MatchNumber = Random.Shared.Next(1, 10_000),
            Phase = MatchPhase.GroupStage,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            KickoffTimeUtc = kickoffTimeUtc,
            Status = MatchStatus.Scheduled,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
