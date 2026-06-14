using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Domain.Entities;

public class NotificationDelivery
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid PushSubscriptionId { get; set; }
    public Guid? MatchId { get; set; }
    public string SubjectKey { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public DateTime ScheduledForUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;
    public string? ErrorCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public PushSubscription PushSubscription { get; set; } = null!;
    public Match? Match { get; set; }
}
