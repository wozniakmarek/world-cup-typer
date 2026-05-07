using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface IScoringService
{
    PredictionScoreDto CalculateScore(int predictedHomeScore, int predictedAwayScore, int actualHomeScore, int actualAwayScore);
}
