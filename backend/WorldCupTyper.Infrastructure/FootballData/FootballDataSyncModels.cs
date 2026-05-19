using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Infrastructure.FootballData;

public sealed record FootballDataTeamSyncModel(
    string? ExternalId,
    string Name,
    string ShortName,
    string CountryCode);

public sealed record FootballDataMatchSyncModel(
    string ExternalId,
    int MatchNumber,
    MatchPhase Phase,
    string? GroupName,
    FootballDataTeamSyncModel HomeTeam,
    FootballDataTeamSyncModel AwayTeam,
    DateTime KickoffTimeUtc,
    string? Venue,
    MatchStatus Status,
    int? HomeScore90,
    int? AwayScore90,
    int? HomeScoreFinal,
    int? AwayScoreFinal)
{
    public bool CanSettle => Status == MatchStatus.Finished && HomeScore90.HasValue && AwayScore90.HasValue;
}
