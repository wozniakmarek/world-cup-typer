using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Domain.Entities;

public class Match
{
    public Guid Id { get; set; }
    public string? ExternalId { get; set; }
    public int MatchNumber { get; set; }
    public MatchPhase Phase { get; set; } = MatchPhase.GroupStage;
    public string? GroupName { get; set; }
    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }
    public string? HomeSlotRule { get; set; }
    public string? AwaySlotRule { get; set; }
    public DateTime KickoffTimeUtc { get; set; }
    public string? Venue { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public int? HomeScore90 { get; set; }
    public int? AwayScore90 { get; set; }
    public int? HomeScoreFinal { get; set; }
    public int? AwayScoreFinal { get; set; }
    public Guid? WinnerTeamId { get; set; }
    public bool IsSettled { get; set; }
    public DateTime? SettledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;
    public Team? WinnerTeam { get; set; }
    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    public ICollection<LeaderboardSnapshot> LeaderboardSnapshots { get; set; } = new List<LeaderboardSnapshot>();

    public bool CanAcceptPredictions(DateTime nowUtc) => KickoffTimeUtc > nowUtc;
}
