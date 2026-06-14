namespace WorldCupTyper.Domain.Entities;

public class NotificationPreference
{
    public Guid UserId { get; set; }
    public bool MorningDigestEnabled { get; set; } = true;
    public bool MissingPrediction2hEnabled { get; set; } = true;
    public bool MissingPrediction30mEnabled { get; set; } = true;
    public bool RankingUpdatedEnabled { get; set; } = true;
    public TimeOnly? QuietHoursStartLocal { get; set; }
    public TimeOnly? QuietHoursEndLocal { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
