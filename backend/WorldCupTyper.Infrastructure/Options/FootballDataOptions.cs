namespace WorldCupTyper.Infrastructure.Options;

public sealed class FootballDataOptions
{
    public const string SectionName = "FootballData";

    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.football-data.org/v4/";
    public string ApiToken { get; set; } = string.Empty;
    public string CompetitionCode { get; set; } = "WC";
    public int SyncIntervalMinutes { get; set; } = 30;
    public int LookbackDays { get; set; } = 2;
    public int LookaheadDays { get; set; } = 370;
    public bool SettleAutomatically { get; set; } = true;
}
