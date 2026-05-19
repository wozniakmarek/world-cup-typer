using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.FootballData;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class FootballDataScheduleImportService : IScheduleImportService
{
    private readonly IAppDbContext _dbContext;
    private readonly IFootballDataClient _client;
    private readonly IMatchSettlementService _settlementService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly FootballDataOptions _options;
    private readonly ILogger<FootballDataScheduleImportService> _logger;

    public FootballDataScheduleImportService(
        IAppDbContext dbContext,
        IFootballDataClient client,
        IMatchSettlementService settlementService,
        IDateTimeProvider dateTimeProvider,
        IOptions<FootballDataOptions> options,
        ILogger<FootballDataScheduleImportService> logger)
    {
        _dbContext = dbContext;
        _client = client;
        _settlementService = settlementService;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ScheduleSyncSummaryDto> ImportScheduleAsync(CancellationToken cancellationToken = default)
    {
        var imported = 0;
        var updated = 0;
        var skipped = 0;
        var settled = 0;
        var failed = 0;

        var providerMatches = await _client.GetCompetitionMatchesAsync(cancellationToken);
        foreach (var providerMatch in providerMatches)
        {
            try
            {
                var homeTeam = await UpsertTeamAsync(providerMatch.HomeTeam, cancellationToken);
                var awayTeam = await UpsertTeamAsync(providerMatch.AwayTeam, cancellationToken);
                var (match, wasCreated) = await UpsertMatchAsync(providerMatch, homeTeam.Id, awayTeam.Id, cancellationToken);

                if (wasCreated)
                {
                    imported++;
                }
                else
                {
                    updated++;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                if (_options.SettleAutomatically && CanAutoSettle(match))
                {
                    await _settlementService.SettleMatchAsync(match.Id, cancellationToken);
                    settled++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                _logger.LogWarning(exception, "Football-data.org match import failed for {ExternalId}.", providerMatch.ExternalId);
            }
        }

        return new ScheduleSyncSummaryDto(imported, updated, skipped, settled, failed);
    }

    private async Task<Team> UpsertTeamAsync(FootballDataTeamSyncModel providerTeam, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(providerTeam, cancellationToken);
        if (team is null)
        {
            team = new Team
            {
                Id = Guid.NewGuid(),
                ExternalId = providerTeam.ExternalId,
                Name = providerTeam.Name,
                ShortName = providerTeam.ShortName,
                CountryCode = NormalizeCountryCode(providerTeam.CountryCode),
            };

            await _dbContext.Teams.AddAsync(team, cancellationToken);
            return team;
        }

        team.ExternalId ??= providerTeam.ExternalId;
        team.Name = providerTeam.Name;
        team.ShortName = providerTeam.ShortName;
        team.CountryCode = NormalizeCountryCode(providerTeam.CountryCode);
        return team;
    }

    private async Task<Team?> FindTeamAsync(FootballDataTeamSyncModel providerTeam, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(providerTeam.ExternalId))
        {
            var byExternalId = await _dbContext.Teams
                .FirstOrDefaultAsync(team => team.ExternalId == providerTeam.ExternalId, cancellationToken);
            if (byExternalId is not null)
            {
                return byExternalId;
            }
        }

        var countryCode = NormalizeCountryCode(providerTeam.CountryCode);
        return await _dbContext.Teams.FirstOrDefaultAsync(
            team => team.CountryCode == countryCode
                || team.ShortName == providerTeam.ShortName
                || team.Name == providerTeam.Name,
            cancellationToken);
    }

    private async Task<(Match Match, bool WasCreated)> UpsertMatchAsync(
        FootballDataMatchSyncModel providerMatch,
        Guid homeTeamId,
        Guid awayTeamId,
        CancellationToken cancellationToken)
    {
        var match = await _dbContext.Matches
            .FirstOrDefaultAsync(candidate => candidate.ExternalId == providerMatch.ExternalId, cancellationToken);

        if (match is null)
        {
            match = await _dbContext.Matches
                .FirstOrDefaultAsync(candidate => candidate.MatchNumber == providerMatch.MatchNumber, cancellationToken);
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        if (match is null)
        {
            match = new Match
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = nowUtc,
            };
            await _dbContext.Matches.AddAsync(match, cancellationToken);
            ApplyProviderMatch(match, providerMatch, homeTeamId, awayTeamId, nowUtc);
            return (match, true);
        }

        ApplyProviderMatch(match, providerMatch, homeTeamId, awayTeamId, nowUtc);
        return (match, false);
    }

    private static void ApplyProviderMatch(
        Match match,
        FootballDataMatchSyncModel providerMatch,
        Guid homeTeamId,
        Guid awayTeamId,
        DateTime nowUtc)
    {
        match.ExternalId = providerMatch.ExternalId;
        match.MatchNumber = providerMatch.MatchNumber;
        match.Phase = providerMatch.Phase;
        match.GroupName = providerMatch.GroupName;
        match.HomeTeamId = homeTeamId;
        match.AwayTeamId = awayTeamId;
        match.KickoffTimeUtc = providerMatch.KickoffTimeUtc;
        match.Venue = providerMatch.Venue;
        match.Status = match.IsSettled ? MatchStatus.Settled : providerMatch.Status;
        if (providerMatch.HomeScore90.HasValue && providerMatch.AwayScore90.HasValue)
        {
            match.HomeScore90 = providerMatch.HomeScore90;
            match.AwayScore90 = providerMatch.AwayScore90;
        }

        if (providerMatch.HomeScoreFinal.HasValue && providerMatch.AwayScoreFinal.HasValue)
        {
            match.HomeScoreFinal = providerMatch.HomeScoreFinal;
            match.AwayScoreFinal = providerMatch.AwayScoreFinal;
        }
        match.WinnerTeamId = ResolveWinnerTeamId(
            homeTeamId,
            awayTeamId,
            match.HomeScoreFinal,
            match.AwayScoreFinal,
            match.HomeScore90,
            match.AwayScore90);
        match.UpdatedAtUtc = nowUtc;
    }

    private static bool CanAutoSettle(Match match)
    {
        return match.Status == MatchStatus.Finished
            && !match.IsSettled
            && match.HomeScore90.HasValue
            && match.AwayScore90.HasValue;
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

    private static string NormalizeCountryCode(string countryCode)
    {
        return countryCode.Trim().ToUpperInvariant();
    }
}
