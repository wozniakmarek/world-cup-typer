using WorldCupTyper.Application.Abstractions;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class StubScheduleImportService : IScheduleImportService
{
    public Task ImportScheduleAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
