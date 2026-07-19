using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Api.Controllers;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

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
    public void GetFinalSummaryAvailability_ShouldAllowAnonymousUsers()
    {
        var method = typeof(SummaryController).GetMethod(nameof(SummaryController.GetFinalSummaryAvailability));

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
        var availability = new FinalSummaryAvailabilityDto(false, "final-match-not-settled", 102, 104, 104, "ARG-ESP");
        var response = new FinalSummaryResponseDto(
            new FinalSummaryStatsDto(76, 24, userId, "Marek"),
            new[] { series },
            new[] { new FinalRankingEntryDto(userId, "Marek", null, 1, 121, 24, 73, 104, false) },
            new[] { fact });
        var personal = new PersonalFinalSummaryResponseDto(userId, "Marek", null, 1, 121, 24, 73, 104, new[] { fact }, new[] { matchId });

        availability.IsReady.Should().BeFalse();
        availability.Reason.Should().Be("final-match-not-settled");
        response.PositionSeries.Single().Points.Single().Position.Should().Be(2);
        response.GlobalFacts.Single().Id.Should().Be("biggest-climb");
        personal.PersonalFacts.Single().Title.Should().Be("Awans o 9 miejsc");
    }

    [Fact]
    public async Task GetFinalSummary_ShouldReturnConflictWhenFinalRecapIsNotReady()
    {
        var service = new StubFinalSummaryService(new FinalSummaryAvailabilityDto(false, "matches-still-open", 102, 104, 104, "ARG-ESP"));
        var controller = new SummaryController(service);

        var result = await controller.GetFinalSummary(CancellationToken.None);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new
        {
            message = "Finalny recap będzie dostępny po rozliczeniu finału.",
            availability = service.Availability,
        });
        service.FinalSummaryCalls.Should().Be(0);
    }

    [Fact]
    public async Task GetMyFinalSummary_ShouldReturnConflictWhenFinalRecapIsNotReady()
    {
        var service = new StubFinalSummaryService(new FinalSummaryAvailabilityDto(false, "matches-still-open", 102, 104, 104, "ARG-ESP"));
        var controller = new SummaryController(service);

        var result = await controller.GetMyFinalSummary(CancellationToken.None);

        var conflict = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new
        {
            message = "Finalny recap będzie dostępny po rozliczeniu finału.",
            availability = service.Availability,
        });
        service.PersonalSummaryCalls.Should().Be(0);
    }

    private sealed class StubFinalSummaryService : IFinalSummaryService
    {
        public StubFinalSummaryService(FinalSummaryAvailabilityDto availability)
        {
            Availability = availability;
        }

        public FinalSummaryAvailabilityDto Availability { get; }
        public int FinalSummaryCalls { get; private set; }
        public int PersonalSummaryCalls { get; private set; }

        public Task<FinalSummaryAvailabilityDto> GetFinalSummaryAvailabilityAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Availability);
        }

        public Task<FinalSummaryResponseDto> GetFinalSummaryAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default)
        {
            FinalSummaryCalls++;
            throw new InvalidOperationException("Summary should not be loaded before the recap is ready.");
        }

        public Task<PersonalFinalSummaryResponseDto> GetPersonalFinalSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            PersonalSummaryCalls++;
            throw new InvalidOperationException("Personal summary should not be loaded before the recap is ready.");
        }
    }
}
