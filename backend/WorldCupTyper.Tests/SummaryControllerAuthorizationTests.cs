using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using WorldCupTyper.Api.Controllers;
using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Tests;

public sealed class SummaryControllerAuthorizationTests
{
    [Fact]
    public void SummaryController_ShouldRequireAuthenticatedUserByDefault()
    {
        typeof(SummaryController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public void GetFinalSummary_ShouldAllowAnonymousUsers()
    {
        var method = typeof(SummaryController).GetMethod(nameof(SummaryController.GetFinalSummary));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Should().NotBeEmpty();
    }

    [Fact]
    public void GetMyFinalSummary_ShouldStayAuthenticatedOnly()
    {
        var method = typeof(SummaryController).GetMethod(nameof(SummaryController.GetMyFinalSummary));

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Should().BeEmpty();
    }

    [Fact]
    public void FinalSummaryDtos_ShouldExposeChartFactsAndPersonalRecap()
    {
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var point = new FinalRankingPositionPointDto(matchId, 1, "POL-GER", DateTime.UnixEpoch, 2, 6);
        var series = new FinalRankingPositionSeriesDto(userId, "Marek", null, 1, 121, false, new[] { point });
        var fact = new FinalSummaryFactDto("biggest-climb", "Skok", "Awans o 9 miejsc", "Opis", new[] { userId }, new[] { matchId });
        var response = new FinalSummaryResponseDto(
            new FinalSummaryStatsDto(76, 24, userId, "Marek"),
            new[] { series },
            new[] { new FinalRankingEntryDto(userId, "Marek", null, 1, 121, 24, 73, 104, false) },
            new[] { fact });
        var personal = new PersonalFinalSummaryResponseDto(userId, "Marek", null, 1, 121, 24, 73, 104, new[] { fact }, new[] { matchId });

        response.PositionSeries.Single().Points.Single().Position.Should().Be(2);
        response.GlobalFacts.Single().Id.Should().Be("biggest-climb");
        personal.PersonalFacts.Single().Title.Should().Be("Awans o 9 miejsc");
    }
}
