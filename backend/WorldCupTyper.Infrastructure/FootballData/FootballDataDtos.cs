using System.Text.Json.Serialization;

namespace WorldCupTyper.Infrastructure.FootballData;

public sealed class FootballDataMatchesResponseDto
{
    [JsonPropertyName("matches")]
    public IReadOnlyCollection<FootballDataMatchDto> Matches { get; set; } = [];
}

public sealed class FootballDataMatchDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("matchday")]
    public int? Matchday { get; set; }

    [JsonPropertyName("stage")]
    public string? Stage { get; set; }

    [JsonPropertyName("group")]
    public string? Group { get; set; }

    [JsonPropertyName("utcDate")]
    public DateTime UtcDate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("homeTeam")]
    public FootballDataTeamDto HomeTeam { get; set; } = new();

    [JsonPropertyName("awayTeam")]
    public FootballDataTeamDto AwayTeam { get; set; } = new();

    [JsonPropertyName("score")]
    public FootballDataScoreDto Score { get; set; } = new();

    [JsonPropertyName("venue")]
    public string? Venue { get; set; }
}

public sealed class FootballDataTeamDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("tla")]
    public string? Tla { get; set; }
}

public sealed class FootballDataScoreDto
{
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("fullTime")]
    public FootballDataScorePartDto FullTime { get; set; } = new();

    [JsonPropertyName("regularTime")]
    public FootballDataScorePartDto RegularTime { get; set; } = new();
}

public sealed class FootballDataScorePartDto
{
    [JsonPropertyName("home")]
    public int? Home { get; set; }

    [JsonPropertyName("away")]
    public int? Away { get; set; }
}
