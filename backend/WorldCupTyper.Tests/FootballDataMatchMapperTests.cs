using FluentAssertions;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.FootballData;

namespace WorldCupTyper.Tests;

public sealed class FootballDataMatchMapperTests
{
    [Fact]
    public void Map_WithScheduledStatus_ShouldMapScheduled()
    {
        var match = CreateMatch(status: "SCHEDULED");

        var result = FootballDataMatchMapper.Map(match);

        result.Should().NotBeNull();
        result!.ExternalId.Should().Be("football-data:1001");
        result.Status.Should().Be(MatchStatus.Scheduled);
        result.MatchNumber.Should().Be(1001);
        result.Phase.Should().Be(MatchPhase.GroupStage);
        result.GroupName.Should().Be("C");
    }

    [Fact]
    public void Map_WithInPlayStatus_ShouldMapInProgress()
    {
        var match = CreateMatch(status: "IN_PLAY");

        var result = FootballDataMatchMapper.Map(match);

        result.Should().NotBeNull();
        result!.Status.Should().Be(MatchStatus.InProgress);
    }

    [Fact]
    public void Map_WithInPlayFullTimeScore_ShouldNotTreatLiveScoreAsFinalResult()
    {
        var match = CreateMatch(
            status: "IN_PLAY",
            duration: "REGULAR",
            fullHome: 0,
            fullAway: 1);

        var result = FootballDataMatchMapper.Map(match);

        result.Should().NotBeNull();
        result!.Status.Should().Be(MatchStatus.InProgress);
        result.HomeScore90.Should().BeNull();
        result.AwayScore90.Should().BeNull();
        result.HomeScoreFinal.Should().BeNull();
        result.AwayScoreFinal.Should().BeNull();
        result.CanSettle.Should().BeFalse();
    }

    [Fact]
    public void Map_WithRegularTimeScore_ShouldUseRegularTimeForScore90()
    {
        var match = CreateMatch(
            status: "FINISHED",
            duration: "EXTRA_TIME",
            regularHome: 1,
            regularAway: 1,
            fullHome: 2,
            fullAway: 1);

        var result = FootballDataMatchMapper.Map(match);

        result.Should().NotBeNull();
        result!.HomeScore90.Should().Be(1);
        result.AwayScore90.Should().Be(1);
        result.HomeScoreFinal.Should().Be(2);
        result.AwayScoreFinal.Should().Be(1);
        result.CanSettle.Should().BeTrue();
    }

    [Fact]
    public void Map_WithRegularDurationAndNoRegularTime_ShouldUseFullTimeForScore90()
    {
        var match = CreateMatch(
            status: "FINISHED",
            duration: "REGULAR",
            regularHome: null,
            regularAway: null,
            fullHome: 3,
            fullAway: 0);

        var result = FootballDataMatchMapper.Map(match);

        result.Should().NotBeNull();
        result!.HomeScore90.Should().Be(3);
        result.AwayScore90.Should().Be(0);
        result.CanSettle.Should().BeTrue();
    }

    [Fact]
    public void Map_WithExtraTimeAndNoRegularTime_ShouldLeaveScore90Empty()
    {
        var match = CreateMatch(
            status: "FINISHED",
            duration: "PENALTY_SHOOTOUT",
            regularHome: null,
            regularAway: null,
            fullHome: 1,
            fullAway: 1);

        var result = FootballDataMatchMapper.Map(match);

        result.Should().NotBeNull();
        result!.HomeScore90.Should().BeNull();
        result.AwayScore90.Should().BeNull();
        result.HomeScoreFinal.Should().Be(1);
        result.AwayScoreFinal.Should().Be(1);
        result.CanSettle.Should().BeFalse();
    }

    [Fact]
    public void Map_WithUnknownStatus_ShouldReturnNull()
    {
        var match = CreateMatch(status: "MYSTERY");

        var result = FootballDataMatchMapper.Map(match);

        result.Should().BeNull();
    }

    [Fact]
    public void Map_WithMissingTeamTla_ShouldConstrainTeamCodesToDatabaseLimits()
    {
        var match = CreateMatch(status: "SCHEDULED");
        match.HomeTeam.Tla = null;
        match.HomeTeam.ShortName = "Winner Group A";

        var result = FootballDataMatchMapper.Map(match);

        result.Should().NotBeNull();
        result!.HomeTeam.ShortName.Should().Be("Winner Group A");
        result.HomeTeam.CountryCode.Should().Be("WGA");
    }

    private static FootballDataMatchDto CreateMatch(
        string status,
        string duration = "REGULAR",
        int? regularHome = null,
        int? regularAway = null,
        int? fullHome = null,
        int? fullAway = null)
    {
        return new FootballDataMatchDto
        {
            Id = 1001,
            Matchday = 12,
            Stage = "GROUP_STAGE",
            Group = "Group C",
            UtcDate = new DateTime(2026, 6, 18, 18, 0, 0, DateTimeKind.Utc),
            Status = status,
            HomeTeam = new FootballDataTeamDto
            {
                Id = 794,
                Name = "Poland",
                ShortName = "Poland",
                Tla = "POL",
            },
            AwayTeam = new FootballDataTeamDto
            {
                Id = 759,
                Name = "Germany",
                ShortName = "Germany",
                Tla = "GER",
            },
            Score = new FootballDataScoreDto
            {
                Duration = duration,
                RegularTime = new FootballDataScorePartDto { Home = regularHome, Away = regularAway },
                FullTime = new FootballDataScorePartDto { Home = fullHome, Away = fullAway },
            },
            Venue = "MetLife Stadium",
        };
    }
}
