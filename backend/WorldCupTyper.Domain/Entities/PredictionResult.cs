namespace WorldCupTyper.Domain.Entities;

public class PredictionResult
{
    public Guid Id { get; set; }
    public Guid PredictionId { get; set; }
    public int Points { get; set; }
    public bool IsExactScore { get; set; }
    public bool IsCorrectOutcome { get; set; }
    public DateTime CalculatedAtUtc { get; set; }

    public Prediction Prediction { get; set; } = null!;
}
