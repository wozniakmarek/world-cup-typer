namespace WorldCupTyper.Infrastructure.Options;

public sealed class NotificationReminderOptions
{
    public const string SectionName = "NotificationReminders";

    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 5;
}
