using WorldCupTyper.Domain.Enums;

namespace WorldCupTyper.Domain.Entities;

public class ApplicationUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Player;
    public bool IsActive { get; set; } = true;
    public bool RequiresPasswordChange { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    public ICollection<LeaderboardSnapshot> LeaderboardSnapshots { get; set; } = new List<LeaderboardSnapshot>();
    public NotificationPreference? NotificationPreference { get; set; }
    public ICollection<PushSubscription> PushSubscriptions { get; set; } = new List<PushSubscription>();
    public ICollection<NotificationDelivery> NotificationDeliveries { get; set; } = new List<NotificationDelivery>();
}
