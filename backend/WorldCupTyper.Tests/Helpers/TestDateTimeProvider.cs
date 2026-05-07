using WorldCupTyper.Application.Abstractions;

namespace WorldCupTyper.Tests.Helpers;

internal sealed class TestDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
}
