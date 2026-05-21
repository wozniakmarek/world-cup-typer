using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.FootballData;
using WorldCupTyper.Infrastructure.Options;
using WorldCupTyper.Infrastructure.Services;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class FootballDataScheduleImportServiceTests
{
    [Fact]
    public async Task ImportScheduleAsync_WithExistingFinishedMatch_ShouldUpdateScoresAndSettlePredictions()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var homeTeam = CreateTeam("football-data:794", "Poland", "POL");
        var awayTeam = CreateTeam("football-data:759", "Germany", "GER");
        var match = CreateMatch("football-data:1001", homeTeam.Id, awayTeam.Id);
        var user = CreateUser();
        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            MatchId = match.Id,
            PredictedHomeScore = 2,
            PredictedAwayScore = 0,
            CreatedAtUtc = dateTimeProvider.UtcNow.AddDays(-1),
        };

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Users.Add(user);
        dbContext.Matches.Add(match);
        dbContext.Predictions.Add(prediction);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            dateTimeProvider,
            Match(status: MatchStatus.Finished, homeScore90: 2, awayScore90: 0, homeScoreFinal: 2, awayScoreFinal: 0));

        var summary = await service.ImportScheduleAsync();

        summary.UpdatedMatches.Should().Be(1);
        summary.SettledMatches.Should().Be(1);
        var savedMatch = dbContext.Matches.Single(candidate => candidate.Id == match.Id);
        savedMatch.HomeScore90.Should().Be(2);
        savedMatch.AwayScore90.Should().Be(0);
        savedMatch.IsSettled.Should().BeTrue();
        dbContext.PredictionResults.Single().Points.Should().Be(3);
        dbContext.LeaderboardSnapshots.Should().ContainSingle();
    }

    [Fact]
    public async Task ImportScheduleAsync_RunTwice_ShouldNotDuplicateTeamsOrMatches()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var providerMatch = Match(status: MatchStatus.Scheduled);
        var service = CreateService(dbContext, dateTimeProvider, providerMatch);

        await service.ImportScheduleAsync();
        await service.ImportScheduleAsync();

        dbContext.Teams.Should().HaveCount(2);
        dbContext.Matches.Should().ContainSingle();
    }

    [Fact]
    public async Task ImportScheduleAsync_WithSamePlaceholderTeamInOneMatch_ShouldReuseTrackedTeam()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var placeholderTeam = new FootballDataTeamSyncModel(null, "To be announced", "TBA", "TBA");
        var providerMatch = Match(
            status: MatchStatus.Scheduled,
            homeTeam: placeholderTeam,
            awayTeam: placeholderTeam);
        var service = CreateService(dbContext, dateTimeProvider, providerMatch);

        var summary = await service.ImportScheduleAsync();

        summary.FailedMatches.Should().Be(0);
        dbContext.Teams.Should().ContainSingle();
        var savedMatch = dbContext.Matches.Single();
        savedMatch.HomeTeamId.Should().Be(savedMatch.AwayTeamId);
    }

    [Fact]
    public async Task ImportScheduleAsync_WithGroupStageTeams_ShouldApplyGroupAndFlags()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var providerMatch = Match(status: MatchStatus.Scheduled, groupName: "A");
        var service = CreateService(dbContext, dateTimeProvider, providerMatch);

        var summary = await service.ImportScheduleAsync();

        summary.FailedMatches.Should().Be(0);
        dbContext.Teams.Single(team => team.Name == "Poland").GroupName.Should().Be("A");
        dbContext.Teams.Single(team => team.Name == "Poland").FlagEmoji.Should().Be("🇵🇱");
        dbContext.Teams.Single(team => team.Name == "Germany").GroupName.Should().Be("A");
        dbContext.Teams.Single(team => team.Name == "Germany").FlagEmoji.Should().Be("🇩🇪");
    }

    [Fact]
    public async Task ImportScheduleAsync_WithUnlistedIsoCountryCode_ShouldDeriveFlagEmoji()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var providerMatch = Match(
            status: MatchStatus.Scheduled,
            homeTeam: new FootballDataTeamSyncModel("football-data:100", "Nigeria", "NGA", "NGA"),
            awayTeam: new FootballDataTeamSyncModel("football-data:101", "Norway", "NOR", "NOR"));
        var service = CreateService(dbContext, dateTimeProvider, providerMatch);

        var summary = await service.ImportScheduleAsync();

        summary.FailedMatches.Should().Be(0);
        dbContext.Teams.Single(team => team.Name == "Nigeria").FlagEmoji.Should().Be("🇳🇬");
        dbContext.Teams.Single(team => team.Name == "Norway").FlagEmoji.Should().Be("🇳🇴");
    }

    [Fact]
    public async Task ImportScheduleAsync_WithFifaAliasCountryCode_ShouldResolveFlagEmoji()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        (string Name, string Code, string Flag)[] teams =
        {
            ("Algeria", "ALG", "🇩🇿"),
            ("Cape Verde Islands", "CPV", "🇨🇻"),
            ("Congo DR", "COD", "🇨🇩"),
            ("Curaçao", "CUW", "🇨🇼"),
            ("Haiti", "HAI", "🇭🇹"),
            ("Honduras", "HON", "🇭🇳"),
            ("Iraq", "IRQ", "🇮🇶"),
            ("Jordan", "JOR", "🇯🇴"),
            ("Saudi Arabia", "KSA", "🇸🇦"),
            ("Sweden", "SWE", "🇸🇪"),
            ("Turkey", "TUR", "🇹🇷"),
            ("Uruguay", "URY", "🇺🇾"),
            ("Uzbekistan", "UZB", "🇺🇿"),
        };
        var providerMatches = teams
            .Chunk(2)
            .Select((pair, index) => Match(
                status: MatchStatus.Scheduled,
                matchNumber: 200 + index,
                homeTeam: ToProviderTeam(pair[0], 200 + index * 2),
                awayTeam: pair.Length > 1
                    ? ToProviderTeam(pair[1], 201 + index * 2)
                    : new FootballDataTeamSyncModel("football-data:999", "Poland", "POL", "POL")))
            .ToArray();
        var service = CreateService(dbContext, dateTimeProvider, providerMatches);

        var summary = await service.ImportScheduleAsync();

        summary.FailedMatches.Should().Be(0);
        foreach (var team in teams)
        {
            dbContext.Teams.Single(candidate => candidate.Name == team.Name).FlagEmoji.Should().Be(team.Flag);
        }
    }

    [Fact]
    public async Task ImportScheduleAsync_WithFinishedExtraTimeMatchWithoutRegularTime_ShouldNotSettle()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var service = CreateService(
            dbContext,
            dateTimeProvider,
            Match(status: MatchStatus.Finished, homeScore90: null, awayScore90: null, homeScoreFinal: 1, awayScoreFinal: 1));

        var summary = await service.ImportScheduleAsync();

        summary.ImportedMatches.Should().Be(1);
        summary.SettledMatches.Should().Be(0);
        var savedMatch = dbContext.Matches.Single();
        savedMatch.IsSettled.Should().BeFalse();
        savedMatch.HomeScore90.Should().BeNull();
        savedMatch.AwayScore90.Should().BeNull();
    }

    [Fact]
    public async Task ImportScheduleAsync_WhenProviderLacksSafeScore90_ShouldNotClearExistingScores()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var homeTeam = CreateTeam("football-data:794", "Poland", "POL");
        var awayTeam = CreateTeam("football-data:759", "Germany", "GER");
        var match = CreateMatch("football-data:1001", homeTeam.Id, awayTeam.Id);
        match.HomeScore90 = 2;
        match.AwayScore90 = 1;
        match.HomeScoreFinal = 2;
        match.AwayScoreFinal = 1;
        match.WinnerTeamId = homeTeam.Id;
        match.Status = MatchStatus.Finished;

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            dateTimeProvider,
            Match(status: MatchStatus.Finished, homeScore90: null, awayScore90: null, homeScoreFinal: null, awayScoreFinal: null));

        await service.ImportScheduleAsync();

        var savedMatch = dbContext.Matches.Single(candidate => candidate.Id == match.Id);
        savedMatch.HomeScore90.Should().Be(2);
        savedMatch.AwayScore90.Should().Be(1);
        savedMatch.HomeScoreFinal.Should().Be(2);
        savedMatch.AwayScoreFinal.Should().Be(1);
        savedMatch.WinnerTeamId.Should().Be(homeTeam.Id);
    }

    [Fact]
    public async Task ImportScheduleAsync_WhenSettledMatchProviderLacksScores_ShouldNotClearWinner()
    {
        using var dbContext = TestDbContextFactory.Create();
        var dateTimeProvider = new TestDateTimeProvider();
        var homeTeam = CreateTeam("football-data:794", "Poland", "POL");
        var awayTeam = CreateTeam("football-data:759", "Germany", "GER");
        var match = CreateMatch("football-data:1001", homeTeam.Id, awayTeam.Id);
        match.HomeScore90 = 2;
        match.AwayScore90 = 1;
        match.HomeScoreFinal = 2;
        match.AwayScoreFinal = 1;
        match.WinnerTeamId = homeTeam.Id;
        match.Status = MatchStatus.Settled;
        match.IsSettled = true;
        match.SettledAtUtc = dateTimeProvider.UtcNow;

        dbContext.Teams.AddRange(homeTeam, awayTeam);
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        var service = CreateService(
            dbContext,
            dateTimeProvider,
            Match(status: MatchStatus.Finished, homeScore90: null, awayScore90: null, homeScoreFinal: null, awayScoreFinal: null));

        await service.ImportScheduleAsync();

        var savedMatch = dbContext.Matches.Single(candidate => candidate.Id == match.Id);
        savedMatch.Status.Should().Be(MatchStatus.Settled);
        savedMatch.WinnerTeamId.Should().Be(homeTeam.Id);
    }

    private static FootballDataScheduleImportService CreateService(
        WorldCupTyper.Infrastructure.Persistence.WorldCupTyperDbContext dbContext,
        TestDateTimeProvider dateTimeProvider,
        params FootballDataMatchSyncModel[] matches)
    {
        var settlementService = new MatchSettlementService(
            dbContext,
            new ScoringService(),
            new LeaderboardBuilder(dbContext),
            dateTimeProvider);

        return new FootballDataScheduleImportService(
            dbContext,
            new FakeFootballDataClient(matches),
            settlementService,
            dateTimeProvider,
            Options.Create(new FootballDataOptions { SettleAutomatically = true }),
            NullLogger<FootballDataScheduleImportService>.Instance);
    }

    private static FootballDataMatchSyncModel Match(
        MatchStatus status,
        int? homeScore90 = null,
        int? awayScore90 = null,
        int? homeScoreFinal = null,
        int? awayScoreFinal = null,
        int matchNumber = 1,
        string? groupName = "Group A",
        FootballDataTeamSyncModel? homeTeam = null,
        FootballDataTeamSyncModel? awayTeam = null)
    {
        return new FootballDataMatchSyncModel(
            ExternalId: $"football-data:{matchNumber}",
            MatchNumber: matchNumber,
            Phase: MatchPhase.GroupStage,
            GroupName: groupName,
            HomeTeam: homeTeam ?? new FootballDataTeamSyncModel("football-data:794", "Poland", "POL", "POL"),
            AwayTeam: awayTeam ?? new FootballDataTeamSyncModel("football-data:759", "Germany", "GER", "GER"),
            KickoffTimeUtc: new DateTime(2026, 6, 18, 18, 0, 0, DateTimeKind.Utc),
            Venue: "MetLife Stadium",
            Status: status,
            HomeScore90: homeScore90,
            AwayScore90: awayScore90,
            HomeScoreFinal: homeScoreFinal,
            AwayScoreFinal: awayScoreFinal);
    }

    private static FootballDataTeamSyncModel ToProviderTeam((string Name, string Code, string Flag) team, int externalId)
    {
        return new FootballDataTeamSyncModel($"football-data:{externalId}", team.Name, team.Code, team.Code);
    }

    private static Team CreateTeam(string externalId, string name, string shortName)
    {
        return new Team
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Name = name,
            ShortName = shortName,
            CountryCode = shortName,
        };
    }

    private static Match CreateMatch(string externalId, Guid homeTeamId, Guid awayTeamId)
    {
        return new Match
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            MatchNumber = 1,
            Phase = MatchPhase.GroupStage,
            GroupName = "Group A",
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            KickoffTimeUtc = new DateTime(2026, 6, 18, 18, 0, 0, DateTimeKind.Utc),
            Status = MatchStatus.Scheduled,
            CreatedAtUtc = DateTime.UtcNow,
        };
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

    private sealed class FakeFootballDataClient : IFootballDataClient
    {
        private readonly IReadOnlyCollection<FootballDataMatchSyncModel> _matches;

        public FakeFootballDataClient(IReadOnlyCollection<FootballDataMatchSyncModel> matches)
        {
            _matches = matches;
        }

        public Task<IReadOnlyCollection<FootballDataMatchSyncModel>> GetCompetitionMatchesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_matches);
        }
    }
}
