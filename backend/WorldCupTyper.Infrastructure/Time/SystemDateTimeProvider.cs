using WorldCupTyper.Application.Abstractions;

namespace WorldCupTyper.Infrastructure.Time;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
