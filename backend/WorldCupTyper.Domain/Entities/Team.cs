namespace WorldCupTyper.Domain.Entities;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? FlagEmoji { get; set; }
    public string? GroupName { get; set; }

    public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
    public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
}
