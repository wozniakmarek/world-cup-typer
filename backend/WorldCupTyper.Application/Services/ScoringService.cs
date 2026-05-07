using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Application.Services;

public sealed class ScoringService : IScoringService
{
    public PredictionScoreDto CalculateScore(int predictedHomeScore, int predictedAwayScore, int actualHomeScore, int actualAwayScore)
    {
        EnsureNonNegative(predictedHomeScore, predictedAwayScore, actualHomeScore, actualAwayScore);

        var isExactScore = predictedHomeScore == actualHomeScore && predictedAwayScore == actualAwayScore;
        if (isExactScore)
        {
            return new PredictionScoreDto(3, true, true);
        }

        var predictedOutcome = Math.Sign(predictedHomeScore - predictedAwayScore);
        var actualOutcome = Math.Sign(actualHomeScore - actualAwayScore);
        var isCorrectOutcome = predictedOutcome == actualOutcome;

        return isCorrectOutcome
            ? new PredictionScoreDto(1, false, true)
            : new PredictionScoreDto(0, false, false);
    }

    private static void EnsureNonNegative(params int[] values)
    {
        if (values.Any(value => value < 0))
        {
            throw new BusinessRuleException("Wyniki i typy nie mogą być ujemne.");
        }
    }
}
