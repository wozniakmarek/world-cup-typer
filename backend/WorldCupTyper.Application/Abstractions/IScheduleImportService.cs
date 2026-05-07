namespace WorldCupTyper.Application.Abstractions;

public interface IScheduleImportService
{
    Task ImportScheduleAsync(CancellationToken cancellationToken = default);
}
