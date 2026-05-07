namespace WorldCupTyper.Domain.Entities;

public class LeaderboardSnapshot
{
    public Guid Id { get; set; }
    public Guid MatchId { get; set; }
    public Guid UserId { get; set; }
    public int TotalPoints { get; set; }
    public int ExactScoreHits { get; set; }
    public int CorrectOutcomeHits { get; set; }
    public int PredictionsCount { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Match Match { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
