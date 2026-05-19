using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Infrastructure.FootballData;

public static class FootballDataMatchMapper
{
    private const string ProviderPrefix = "football-data";

    public static FootballDataMatchSyncModel? Map(FootballDataMatchDto match)
    {
        var status = MapStatus(match.Status);
        if (status is null)
        {
            return null;
        }

        var (homeScore90, awayScore90) = ResolveScore90(match.Score);

        return new FootballDataMatchSyncModel(
            ExternalId: BuildExternalId(match.Id),
            MatchNumber: match.Id,
            Phase: MapPhase(match.Stage),
            GroupName: Normalize(match.Group),
            HomeTeam: MapTeam(match.HomeTeam),
            AwayTeam: MapTeam(match.AwayTeam),
            KickoffTimeUtc: DateTime.SpecifyKind(match.UtcDate, DateTimeKind.Utc),
            Venue: Normalize(match.Venue),
            Status: status.Value,
            HomeScore90: homeScore90,
            AwayScore90: awayScore90,
            HomeScoreFinal: match.Score.FullTime.Home,
            AwayScoreFinal: match.Score.FullTime.Away);
    }

    public static string BuildExternalId(int id) => $"{ProviderPrefix}:{id}";

    private static MatchStatus? MapStatus(string? status)
    {
        return NormalizeKey(status) switch
        {
            "SCHEDULED" or "TIMED" => MatchStatus.Scheduled,
            "LIVE" or "IN_PLAY" or "PAUSED" => MatchStatus.InProgress,
            "FINISHED" => MatchStatus.Finished,
            "POSTPONED" or "SUSPENDED" or "CANCELLED" => MatchStatus.Cancelled,
            _ => null,
        };
    }

    private static MatchPhase MapPhase(string? stage)
    {
        return NormalizeKey(stage) switch
        {
            "LAST_32" or "ROUND_OF_32" => MatchPhase.RoundOf32,
            "LAST_16" or "ROUND_OF_16" => MatchPhase.RoundOf16,
            "QUARTER_FINALS" or "QUARTER_FINAL" => MatchPhase.QuarterFinal,
            "SEMI_FINALS" or "SEMI_FINAL" => MatchPhase.SemiFinal,
            "THIRD_PLACE" => MatchPhase.ThirdPlace,
            "FINAL" => MatchPhase.Final,
            _ => MatchPhase.GroupStage,
        };
    }

    private static FootballDataTeamSyncModel MapTeam(FootballDataTeamDto team)
    {
        var name = FirstNonBlank(team.Name, team.ShortName, team.Tla) ?? "Unknown team";
        var shortName = FirstNonBlank(team.Tla, team.ShortName, team.Name) ?? name;
        var countryCode = FirstNonBlank(team.Tla, team.ShortName) ?? shortName;

        return new FootballDataTeamSyncModel(
            ExternalId: team.Id.HasValue ? BuildExternalId(team.Id.Value) : null,
            Name: name.Trim(),
            ShortName: shortName.Trim(),
            CountryCode: countryCode.Trim().ToUpperInvariant());
    }

    private static (int? Home, int? Away) ResolveScore90(FootballDataScoreDto score)
    {
        if (HasBothScores(score.RegularTime))
        {
            return (score.RegularTime.Home, score.RegularTime.Away);
        }

        if (string.Equals(score.Duration, "REGULAR", StringComparison.OrdinalIgnoreCase) && HasBothScores(score.FullTime))
        {
            return (score.FullTime.Home, score.FullTime.Away);
        }

        return (null, null);
    }

    private static bool HasBothScores(FootballDataScorePartDto score)
    {
        return score.Home.HasValue && score.Away.HasValue;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string NormalizeKey(string? value)
    {
        return value?.Trim().ToUpperInvariant() ?? string.Empty;
    }
}
