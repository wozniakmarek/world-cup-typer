using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services;

public sealed class LeaderboardBuilder
{
    private readonly IAppDbContext _dbContext;

    public LeaderboardBuilder(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<LeaderboardEntryDto>> BuildAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(user => user.Id).ToList();
        var settledPredictions = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => userIds.Contains(prediction.UserId) && prediction.Result != null)
            .Select(prediction => new
            {
                prediction.UserId,
                prediction.Result!.Points,
                prediction.Result.IsExactScore,
                prediction.Result.IsCorrectOutcome,
            })
            .ToListAsync(cancellationToken);

        return users
            .Select(user =>
            {
                var predictions = settledPredictions.Where(prediction => prediction.UserId == user.Id).ToList();
                return new
                {
                    user.Id,
                    user.DisplayName,
                    TotalPoints = predictions.Sum(prediction => prediction.Points),
                    ExactScoreHits = predictions.Count(prediction => prediction.IsExactScore),
                    CorrectOutcomeHits = predictions.Count(prediction => prediction.IsCorrectOutcome),
                    PredictionsCount = predictions.Count,
                };
            })
            .OrderByDescending(entry => entry.TotalPoints)
            .ThenByDescending(entry => entry.ExactScoreHits)
            .ThenByDescending(entry => entry.CorrectOutcomeHits)
            .ThenByDescending(entry => entry.PredictionsCount)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select((entry, index) => new LeaderboardEntryDto(
                index + 1,
                entry.Id,
                entry.DisplayName,
                entry.TotalPoints,
                entry.ExactScoreHits,
                entry.CorrectOutcomeHits,
                entry.PredictionsCount,
                currentUserId.HasValue && currentUserId.Value == entry.Id))
            .ToList();
    }
}
