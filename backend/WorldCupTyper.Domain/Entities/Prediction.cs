namespace WorldCupTyper.Domain.Entities;

public class Prediction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MatchId { get; set; }
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? LockedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Match Match { get; set; } = null!;
    public PredictionResult? Result { get; set; }
}
