using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Mappers;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Application.Services;

public sealed class MatchService : IMatchService
{
    private readonly IAppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MatchService(IAppDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyCollection<MatchSummaryDto>> GetMatchesAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var matches = await QueryMatchesForUser(currentUserId)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);

        return matches.Select(match => match.ToMatchSummaryDto(currentUserId, _dateTimeProvider.UtcNow)).ToList();
    }

    public async Task<IReadOnlyCollection<MatchSummaryDto>> GetTodayMatchesAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var start = nowUtc.Date;
        var end = start.AddDays(1);

        var matches = await QueryMatchesForUser(currentUserId)
            .Where(match => match.KickoffTimeUtc >= start && match.KickoffTimeUtc < end)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);

        return matches.Select(match => match.ToMatchSummaryDto(currentUserId, nowUtc)).ToList();
    }

    public async Task<IReadOnlyCollection<MatchSummaryDto>> GetUpcomingMatchesAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var matches = await QueryMatchesForUser(currentUserId)
            .Where(match => match.KickoffTimeUtc >= nowUtc)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .Take(10)
            .ToListAsync(cancellationToken);

        return matches.Select(match => match.ToMatchSummaryDto(currentUserId, nowUtc)).ToList();
    }

    public async Task<MatchDetailsDto> GetMatchDetailsAsync(Guid matchId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var match = await QueryMatchesForUser(currentUserId)
            .FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new NotFoundException("Nie znaleziono meczu.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var canViewPredictions = isAdmin || !match.CanAcceptPredictions(nowUtc);
        return match.ToMatchDetailsDto(currentUserId, canViewPredictions, nowUtc);
    }

    public async Task<IReadOnlyCollection<AdminMatchDto>> GetAdminMatchesAsync(CancellationToken cancellationToken = default)
    {
        var matches = await _dbContext.Matches
            .AsNoTracking()
            .Include(match => match.HomeTeam)
            .Include(match => match.AwayTeam)
            .Include(match => match.Predictions)
            .OrderBy(match => match.KickoffTimeUtc)
            .ThenBy(match => match.MatchNumber)
            .ToListAsync(cancellationToken);

        return matches.Select(match => match.ToAdminDto()).ToList();
    }

    public async Task<AdminMatchDto> CreateMatchAsync(UpsertMatchRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateMatchRequestAsync(request, null, cancellationToken);
        var match = new Match
        {
            Id = Guid.NewGuid(),
            ExternalId = request.ExternalId?.Trim(),
            MatchNumber = request.MatchNumber,
            Phase = request.Phase,
            GroupName = request.GroupName?.Trim(),
            HomeTeamId = request.HomeTeamId,
            AwayTeamId = request.AwayTeamId,
            HomeSlotRule = request.HomeSlotRule?.Trim(),
            AwaySlotRule = request.AwaySlotRule?.Trim(),
            KickoffTimeUtc = request.KickoffTimeUtc,
            Venue = request.Venue?.Trim(),
            Status = request.Status,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            IsSettled = false,
        };

        await _dbContext.Matches.AddAsync(match, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminMatchAsync(match.Id, cancellationToken);
    }

    public async Task<AdminMatchDto> UpdateMatchAsync(Guid matchId, UpsertMatchRequest request, CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.Matches.FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);
        if (match is null)
        {
            throw new NotFoundException("Nie znaleziono meczu.");
        }

        await ValidateMatchRequestAsync(request, matchId, cancellationToken);

        match.ExternalId = request.ExternalId?.Trim();
        match.MatchNumber = request.MatchNumber;
        match.Phase = request.Phase;
        match.GroupName = request.GroupName?.Trim();
        match.HomeTeamId = request.HomeTeamId;
        match.AwayTeamId = request.AwayTeamId;
        match.HomeSlotRule = request.HomeSlotRule?.Trim();
        match.AwaySlotRule = request.AwaySlotRule?.Trim();
        match.KickoffTimeUtc = request.KickoffTimeUtc;
        match.Venue = request.Venue?.Trim();
        match.Status = request.Status;
        match.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminMatchAsync(matchId, cancellationToken);
    }

    public async Task<AdminMatchDto> SetResultAsync(Guid matchId, SetMatchResultRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HomeScore90 < 0 || request.AwayScore90 < 0 || request.HomeScoreFinal < 0 || request.AwayScoreFinal < 0)
        {
            throw new BusinessRuleException("Wyniki meczu nie mogą być ujemne.");
        }

        var match = await _dbContext.Matches.FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);
        if (match is null)
        {
            throw new NotFoundException("Nie znaleziono meczu.");
        }

        match.HomeScore90 = request.HomeScore90;
        match.AwayScore90 = request.AwayScore90;
        match.HomeScoreFinal = request.HomeScoreFinal;
        match.AwayScoreFinal = request.AwayScoreFinal;
        match.WinnerTeamId = request.WinnerTeamId ?? ResolveWinnerTeamId(match.HomeTeamId, match.AwayTeamId, request.HomeScoreFinal, request.AwayScoreFinal, request.HomeScore90, request.AwayScore90);
        match.Status = match.IsSettled ? MatchStatus.Settled : MatchStatus.Finished;
        match.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdminMatchAsync(matchId, cancellationToken);
    }

    private IQueryable<Match> QueryMatchesForUser(Guid currentUserId)
    {
        return _dbContext.Matches
            .AsNoTracking()
            .Include(match => match.HomeTeam)
            .Include(match => match.AwayTeam)
            .Include(match => match.Predictions.Where(prediction => prediction.UserId == currentUserId))
            .ThenInclude(prediction => prediction.Result);
    }

    private async Task ValidateMatchRequestAsync(UpsertMatchRequest request, Guid? currentMatchId, CancellationToken cancellationToken)
    {
        if (request.HomeTeamId == request.AwayTeamId)
        {
            throw new BusinessRuleException("Drużyna gospodarzy i gości musi być różna.");
        }

        var teams = await _dbContext.Teams
            .Where(team => team.Id == request.HomeTeamId || team.Id == request.AwayTeamId)
            .ToListAsync(cancellationToken);

        if (teams.Count != 2)
        {
            throw new BusinessRuleException("Obie drużyny meczu muszą istnieć.");
        }

        var matchNumberTaken = await _dbContext.Matches.AnyAsync(
            match => match.MatchNumber == request.MatchNumber && match.Id != currentMatchId,
            cancellationToken);

        if (matchNumberTaken)
        {
            throw new ConflictException("Mecz o takim numerze już istnieje.");
        }
    }

    private async Task<AdminMatchDto> GetAdminMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var match = await _dbContext.Matches
            .AsNoTracking()
            .Include(candidate => candidate.HomeTeam)
            .Include(candidate => candidate.AwayTeam)
            .Include(candidate => candidate.Predictions)
            .FirstOrDefaultAsync(candidate => candidate.Id == matchId, cancellationToken);

        if (match is null)
        {
            throw new NotFoundException("Nie znaleziono meczu.");
        }

        return match.ToAdminDto();
    }

    private static Guid? ResolveWinnerTeamId(Guid homeTeamId, Guid awayTeamId, int? homeFinal, int? awayFinal, int? home90, int? away90)
    {
        var homeScore = homeFinal ?? home90;
        var awayScore = awayFinal ?? away90;

        if (!homeScore.HasValue || !awayScore.HasValue || homeScore == awayScore)
        {
            return null;
        }

        return homeScore > awayScore ? homeTeamId : awayTeamId;
    }
}
