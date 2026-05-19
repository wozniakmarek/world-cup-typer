using FluentAssertions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Tests;

public sealed class FootballDataOptionsTests
{
    [Fact]
    public void Defaults_ShouldKeepAutomationDisabledAndUseWorldCupCompetition()
    {
        var options = new FootballDataOptions();

        options.Enabled.Should().BeFalse();
        options.CompetitionCode.Should().Be("WC");
        options.SyncIntervalMinutes.Should().Be(30);
        options.SettleAutomatically.Should().BeTrue();
    }

    [Fact]
    public void ScheduleSyncSummary_ShouldExposeAllCounters()
    {
        var summary = new ScheduleSyncSummaryDto(1, 2, 3, 4, 5);

        summary.ImportedMatches.Should().Be(1);
        summary.UpdatedMatches.Should().Be(2);
        summary.SkippedMatches.Should().Be(3);
        summary.SettledMatches.Should().Be(4);
        summary.FailedMatches.Should().Be(5);
    }
}
