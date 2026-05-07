using FluentAssertions;
using WorldCupTyper.Application.Services;

namespace WorldCupTyper.Tests;

public sealed class ScoringServiceTests
{
    private readonly ScoringService _service = new();

    [Fact]
    public void ExactScore_ShouldGiveThreePoints()
    {
        var result = _service.CalculateScore(2, 1, 2, 1);

        result.Points.Should().Be(3);
        result.IsExactScore.Should().BeTrue();
        result.IsCorrectOutcome.Should().BeTrue();
    }

    [Fact]
    public void SameWinner_ShouldGiveOnePoint()
    {
        var result = _service.CalculateScore(2, 0, 1, 0);

        result.Points.Should().Be(1);
        result.IsExactScore.Should().BeFalse();
        result.IsCorrectOutcome.Should().BeTrue();
    }

    [Fact]
    public void CorrectDrawOutcome_ShouldGiveOnePoint()
    {
        var result = _service.CalculateScore(1, 1, 0, 0);

        result.Points.Should().Be(1);
        result.IsExactScore.Should().BeFalse();
        result.IsCorrectOutcome.Should().BeTrue();
    }

    [Fact]
    public void IncorrectPrediction_ShouldGiveZeroPoints()
    {
        var result = _service.CalculateScore(1, 2, 2, 1);

        result.Points.Should().Be(0);
        result.IsExactScore.Should().BeFalse();
        result.IsCorrectOutcome.Should().BeFalse();
    }
}
