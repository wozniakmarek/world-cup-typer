namespace WorldCupTyper.Domain.Entities;

public class PushSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastSeenAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastFailureAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ICollection<NotificationDelivery> NotificationDeliveries { get; set; } = new List<NotificationDelivery>();
}
