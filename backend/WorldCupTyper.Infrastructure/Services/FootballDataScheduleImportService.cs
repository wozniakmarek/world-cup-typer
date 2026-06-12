using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
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
    private static readonly IReadOnlyDictionary<string, string> FlagByCountryCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["ALG"] = "🇩🇿",
        ["ARG"] = "🇦🇷",
        ["AUS"] = "🇦🇺",
        ["AUT"] = "🇦🇹",
        ["BEL"] = "🇧🇪",
        ["BIH"] = "🇧🇦",
        ["BRA"] = "🇧🇷",
        ["CAN"] = "🇨🇦",
        ["CHI"] = "🇨🇱",
        ["CIV"] = "🇨🇮",
        ["COL"] = "🇨🇴",
        ["COD"] = "🇨🇩",
        ["CPV"] = "🇨🇻",
        ["CRC"] = "🇨🇷",
        ["CRO"] = "🇭🇷",
        ["CUW"] = "🇨🇼",
        ["CZE"] = "🇨🇿",
        ["DEN"] = "🇩🇰",
        ["ECU"] = "🇪🇨",
        ["EGY"] = "🇪🇬",
        ["ENG"] = "🏴",
        ["ESP"] = "🇪🇸",
        ["FRA"] = "🇫🇷",
        ["GER"] = "🇩🇪",
        ["GHA"] = "🇬🇭",
        ["HAI"] = "🇭🇹",
        ["HON"] = "🇭🇳",
        ["IRN"] = "🇮🇷",
        ["IRQ"] = "🇮🇶",
        ["ITA"] = "🇮🇹",
        ["JPN"] = "🇯🇵",
        ["JOR"] = "🇯🇴",
        ["KOR"] = "🇰🇷",
        ["KSA"] = "🇸🇦",
        ["MAR"] = "🇲🇦",
        ["MEX"] = "🇲🇽",
        ["NED"] = "🇳🇱",
        ["NOR"] = "🇳🇴",
        ["NZL"] = "🇳🇿",
        ["PAN"] = "🇵🇦",
        ["PAR"] = "🇵🇾",
        ["POL"] = "🇵🇱",
        ["POR"] = "🇵🇹",
        ["QAT"] = "🇶🇦",
        ["RSA"] = "🇿🇦",
        ["SCO"] = "🏴",
        ["SEN"] = "🇸🇳",
        ["SRB"] = "🇷🇸",
        ["SWE"] = "🇸🇪",
        ["SUI"] = "🇨🇭",
        ["TUN"] = "🇹🇳",
        ["TUR"] = "🇹🇷",
        ["URU"] = "🇺🇾",
        ["URY"] = "🇺🇾",
        ["USA"] = "🇺🇸",
        ["UZB"] = "🇺🇿",
        ["WAL"] = "🏴",
    };

    private static readonly IReadOnlyDictionary<string, string> VenueByMatchup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Group A: Mexico, South Africa, Korea Republic, Czechia
        ["MEX-RSA"] = "Estadio Azteca",
        ["KOR-CZE"] = "Estadio Akron",
        ["CZE-RSA"] = "Mercedes-Benz Stadium",
        ["MEX-KOR"] = "Estadio Akron",
        ["CZE-MEX"] = "Estadio Azteca",
        ["RSA-KOR"] = "Estadio BBVA",

        // Group B: Canada, Bosnia and Herzegovina, Switzerland, Qatar
        ["CAN-BIH"] = "BMO Field",
        ["QAT-SUI"] = "Levi's Stadium",
        ["SUI-BIH"] = "SoFi Stadium",
        ["CAN-QAT"] = "BC Place",
        ["SUI-CAN"] = "BC Place",
        ["BIH-QAT"] = "Lumen Field",

        // Group C: Brazil, Scotland, Morocco, Haiti
        ["HAI-SCO"] = "Gillette Stadium",
        ["BRA-MAR"] = "MetLife Stadium",
        ["BRA-HAI"] = "Lincoln Financial Field",
        ["SCO-MAR"] = "Gillette Stadium",
        ["SCO-BRA"] = "Hard Rock Stadium",
        ["MAR-HAI"] = "Mercedes-Benz Stadium",

        // Group D: United States, Paraguay, Australia, Türkiye
        ["USA-PAR"] = "SoFi Stadium",
        ["AUS-TUR"] = "BC Place",
        ["TUR-PAR"] = "Levi's Stadium",
        ["USA-AUS"] = "Lumen Field",
        ["TUR-USA"] = "SoFi Stadium",
        ["PAR-AUS"] = "Levi's Stadium",

        // Group E: Germany, Côte d'Ivoire, Ecuador, Curaçao
        ["CIV-ECU"] = "Lincoln Financial Field",
        ["GER-CUW"] = "NRG Stadium",
        ["GER-CIV"] = "BMO Field",
        ["ECU-CUW"] = "Arrowhead Stadium",
        ["CUW-CIV"] = "Lincoln Financial Field",
        ["ECU-GER"] = "MetLife Stadium",

        // Group F: Netherlands, Japan, Sweden, Tunisia
        ["NED-JPN"] = "AT&T Stadium",
        ["SWE-TUN"] = "Estadio BBVA",
        ["NED-SWE"] = "NRG Stadium",
        ["TUN-JPN"] = "Estadio BBVA",
        ["JPN-SWE"] = "AT&T Stadium",
        ["TUN-NED"] = "Arrowhead Stadium",

        // Group G: Belgium, Iran, New Zealand, Egypt
        ["IRN-NZL"] = "SoFi Stadium",
        ["BEL-EGY"] = "Lumen Field",
        ["BEL-IRN"] = "SoFi Stadium",
        ["NZL-EGY"] = "BC Place",
        ["EGY-IRN"] = "Lumen Field",
        ["NZL-BEL"] = "BC Place",

        // Group H: Spain, Saudi Arabia, Uruguay, Cabo Verde
        ["KSA-URY"] = "Hard Rock Stadium",
        ["ESP-CPV"] = "Mercedes-Benz Stadium",
        ["URY-CPV"] = "Hard Rock Stadium",
        ["ESP-KSA"] = "Mercedes-Benz Stadium",
        ["CPV-KSA"] = "NRG Stadium",
        ["URY-ESP"] = "Estadio Akron",

        // Group I: France, Senegal, Iraq, Norway
        ["FRA-SEN"] = "MetLife Stadium",
        ["IRQ-NOR"] = "Gillette Stadium",
        ["NOR-SEN"] = "MetLife Stadium",
        ["FRA-IRQ"] = "Lincoln Financial Field",
        ["NOR-FRA"] = "Gillette Stadium",
        ["SEN-IRQ"] = "BMO Field",

        // Group J: Argentina, Algeria, Austria, Jordan
        ["ARG-ALG"] = "Arrowhead Stadium",
        ["AUT-JOR"] = "Levi's Stadium",
        ["ARG-AUT"] = "AT&T Stadium",
        ["JOR-ALG"] = "Levi's Stadium",
        ["ALG-AUT"] = "Arrowhead Stadium",
        ["JOR-ARG"] = "AT&T Stadium",

        // Group K: Portugal, Colombia, Congo DR, Uzbekistan
        ["POR-COD"] = "NRG Stadium",
        ["UZB-COL"] = "Estadio Azteca",
        ["POR-UZB"] = "NRG Stadium",
        ["COL-COD"] = "Estadio Akron",
        ["COL-POR"] = "Hard Rock Stadium",
        ["COD-UZB"] = "Mercedes-Benz Stadium",

        // Group L: England, Panama, Croatia, Ghana
        ["GHA-PAN"] = "BMO Field",
        ["ENG-CRO"] = "AT&T Stadium",
        ["ENG-GHA"] = "Gillette Stadium",
        ["PAN-CRO"] = "BMO Field",
        ["PAN-ENG"] = "MetLife Stadium",
        ["CRO-GHA"] = "Lincoln Financial Field",

        // Uruguay alternate code used by some data providers
        ["KSA-URU"] = "Hard Rock Stadium",
        ["URU-CPV"] = "Hard Rock Stadium",
        ["URU-ESP"] = "Estadio Akron",
    };

    private static readonly IReadOnlyDictionary<string, string> IsoRegionCodeByFifaCode = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures)
        .Select(culture => new RegionInfo(culture.Name))
        .GroupBy(region => region.ThreeLetterISORegionName, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => group.First().TwoLetterISORegionName,
            StringComparer.OrdinalIgnoreCase);

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
                var homeTeam = await UpsertTeamAsync(providerMatch.HomeTeam, providerMatch.GroupName, providerMatch.Phase, cancellationToken);
                var awayTeam = await UpsertTeamAsync(providerMatch.AwayTeam, providerMatch.GroupName, providerMatch.Phase, cancellationToken);
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
                ClearTrackedChangesAfterFailure();
            }
        }

        return new ScheduleSyncSummaryDto(imported, updated, skipped, settled, failed);
    }

    private async Task<Team> UpsertTeamAsync(
        FootballDataTeamSyncModel providerTeam,
        string? groupName,
        MatchPhase phase,
        CancellationToken cancellationToken)
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
                FlagEmoji = ResolveFlagEmoji(providerTeam.CountryCode),
                GroupName = ResolveGroupName(groupName, phase),
            };

            await _dbContext.Teams.AddAsync(team, cancellationToken);
            return team;
        }

        team.ExternalId ??= providerTeam.ExternalId;
        team.Name = providerTeam.Name;
        team.ShortName = providerTeam.ShortName;
        team.CountryCode = NormalizeCountryCode(providerTeam.CountryCode);
        team.FlagEmoji = ResolveFlagEmoji(providerTeam.CountryCode) ?? team.FlagEmoji;
        team.GroupName = ResolveGroupName(groupName, phase) ?? team.GroupName;
        return team;
    }

    private async Task<Team?> FindTeamAsync(FootballDataTeamSyncModel providerTeam, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(providerTeam.ExternalId))
        {
            var trackedByExternalId = _dbContext.Teams.Local
                .FirstOrDefault(team => team.ExternalId == providerTeam.ExternalId);
            if (trackedByExternalId is not null)
            {
                return trackedByExternalId;
            }

            var byExternalId = await _dbContext.Teams
                .FirstOrDefaultAsync(team => team.ExternalId == providerTeam.ExternalId, cancellationToken);
            if (byExternalId is not null)
            {
                return byExternalId;
            }
        }

        var countryCode = NormalizeCountryCode(providerTeam.CountryCode);
        var trackedTeam = _dbContext.Teams.Local.FirstOrDefault(
            team => TeamMatchesProvider(team, providerTeam, countryCode));
        if (trackedTeam is not null)
        {
            return trackedTeam;
        }

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
        match.Venue = providerMatch.Venue
            ?? GetStaticVenue(providerMatch.HomeTeam.CountryCode, providerMatch.AwayTeam.CountryCode)
            ?? match.Venue;
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

    private static string? ResolveFlagEmoji(string countryCode)
    {
        var normalizedCountryCode = NormalizeCountryCode(countryCode);
        if (FlagByCountryCode.TryGetValue(normalizedCountryCode, out var flagEmoji))
        {
            return flagEmoji;
        }

        if (normalizedCountryCode.Length == 2)
        {
            return BuildRegionalIndicatorFlag(normalizedCountryCode);
        }

        return IsoRegionCodeByFifaCode.TryGetValue(normalizedCountryCode, out var isoRegionCode)
            ? BuildRegionalIndicatorFlag(isoRegionCode)
            : null;
    }

    private static string? BuildRegionalIndicatorFlag(string isoRegionCode)
    {
        const int regionalIndicatorOffset = 0x1F1E6 - 'A';
        var normalizedRegionCode = isoRegionCode.Trim().ToUpperInvariant();
        if (normalizedRegionCode.Length != 2 || !normalizedRegionCode.All(character => character is >= 'A' and <= 'Z'))
        {
            return null;
        }

        return string.Concat(normalizedRegionCode.Select(character => char.ConvertFromUtf32(character + regionalIndicatorOffset)));
    }

    private static string? ResolveGroupName(string? groupName, MatchPhase phase)
    {
        return phase == MatchPhase.GroupStage && !string.IsNullOrWhiteSpace(groupName)
            ? groupName.Trim()
            : null;
    }

    private static string? GetStaticVenue(string homeCode, string awayCode)
    {
        return VenueByMatchup.TryGetValue($"{homeCode}-{awayCode}", out var venue) ? venue : null;
    }

    private void ClearTrackedChangesAfterFailure()
    {
        if (_dbContext is DbContext dbContext)
        {
            dbContext.ChangeTracker.Clear();
        }
    }

    private static bool TeamMatchesProvider(Team team, FootballDataTeamSyncModel providerTeam, string countryCode)
    {
        return team.CountryCode == countryCode
            || team.ShortName == providerTeam.ShortName
            || team.Name == providerTeam.Name;
    }
}
