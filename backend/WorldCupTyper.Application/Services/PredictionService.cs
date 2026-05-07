using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Mappers;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Services;

public sealed class PredictionService : IPredictionService
{
    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PredictionService(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<MyPredictionDto>> GetMyPredictionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var predictions = await _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => prediction.UserId == userId)
            .Include(prediction => prediction.Match)
            .ThenInclude(match => match.HomeTeam)
            .Include(prediction => prediction.Match)
            .ThenInclude(match => match.AwayTeam)
            .Include(prediction => prediction.Result)
            .OrderByDescending(prediction => prediction.Match.KickoffTimeUtc)
            .ToListAsync(cancellationToken);

        return predictions
            .Select(prediction => new MyPredictionDto(
                prediction.MatchId,
                prediction.Match.HomeTeam.Name,
                prediction.Match.AwayTeam.Name,
                prediction.Match.KickoffTimeUtc,
                prediction.ToSummaryDto(nowUtc, prediction.Match.KickoffTimeUtc)))
            .ToList();
    }

    public async Task<PredictionSummaryDto> CreatePredictionAsync(Guid userId, Guid matchId, SavePredictionRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePredictionRequest(request);
        await EnsureUserCanPredictAsync(userId, cancellationToken);

        var match = await _dbContext.Matches.FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);
        if (match is null)
        {
            throw new NotFoundException("Nie znaleziono meczu.");
        }

        EnsureKickoffNotPassed(match);

        var existingPrediction = await _dbContext.Predictions.AnyAsync(
            prediction => prediction.UserId == userId && prediction.MatchId == matchId,
            cancellationToken);

        if (existingPrediction)
        {
            throw new ConflictException("Użytkownik ma już zapisany typ dla tego meczu.");
        }

        var prediction = new Prediction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            MatchId = matchId,
            PredictedHomeScore = request.PredictedHomeScore,
            PredictedAwayScore = request.PredictedAwayScore,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
        };

        await _dbContext.Predictions.AddAsync(prediction, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return prediction.ToSummaryDto(_dateTimeProvider.UtcNow, match.KickoffTimeUtc);
    }

    public async Task<PredictionSummaryDto> UpdatePredictionAsync(Guid userId, Guid matchId, SavePredictionRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePredictionRequest(request);
        await EnsureUserCanPredictAsync(userId, cancellationToken);

        var prediction = await _dbContext.Predictions
            .Include(candidate => candidate.Match)
            .Include(candidate => candidate.Result)
            .FirstOrDefaultAsync(candidate => candidate.UserId == userId && candidate.MatchId == matchId, cancellationToken);

        if (prediction is null)
        {
            throw new NotFoundException("Nie znaleziono typu do edycji.");
        }

        EnsureKickoffNotPassed(prediction.Match);

        prediction.PredictedHomeScore = request.PredictedHomeScore;
        prediction.PredictedAwayScore = request.PredictedAwayScore;
        prediction.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return prediction.ToSummaryDto(_dateTimeProvider.UtcNow, prediction.Match.KickoffTimeUtc);
    }

    public async Task<MatchPredictionsResponseDto> GetPredictionsForMatchAsync(Guid requesterUserId, bool isAdmin, Guid matchId, CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new NotFoundException("Nie znaleziono meczu.");
        }

        var canViewAll = isAdmin || !match.CanAcceptPredictions(_dateTimeProvider.UtcNow);

        IQueryable<Prediction> query = _dbContext.Predictions
            .AsNoTracking()
            .Where(prediction => prediction.MatchId == matchId)
            .Include(prediction => prediction.User)
            .Include(prediction => prediction.Result);

        if (!canViewAll)
        {
            query = query.Where(prediction => prediction.UserId == requesterUserId);
        }

        var predictions = await query
            .OrderBy(prediction => prediction.User.DisplayName)
            .ToListAsync(cancellationToken);

        return new MatchPredictionsResponseDto(
            canViewAll,
            predictions.Select(prediction => new MatchPredictionViewDto(
                prediction.Id,
                prediction.UserId,
                prediction.User.DisplayName,
                prediction.PredictedHomeScore,
                prediction.PredictedAwayScore,
                prediction.Result?.Points,
                prediction.Result?.IsExactScore,
                prediction.Result?.IsCorrectOutcome))
            .ToList());
    }

    private async Task EnsureUserCanPredictAsync(Guid userId, CancellationToken cancellationToken)
    {
        var userExists = await _dbContext.Users.AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken);
        if (!userExists)
        {
            throw new UnauthorizedAppException("Nieaktywny użytkownik nie może typować.");
        }
    }

    private void EnsureKickoffNotPassed(Match match)
    {
        if (!match.CanAcceptPredictions(_dateTimeProvider.UtcNow))
        {
            throw new BusinessRuleException("Nie można zmienić typu po rozpoczęciu meczu.");
        }
    }

    private static void ValidatePredictionRequest(SavePredictionRequest request)
    {
        if (request.PredictedHomeScore < 0 || request.PredictedAwayScore < 0)
        {
            throw new BusinessRuleException("Typowane wyniki nie mogą być ujemne.");
        }
    }
}
