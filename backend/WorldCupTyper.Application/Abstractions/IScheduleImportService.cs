namespace WorldCupTyper.Application.Abstractions;

using WorldCupTyper.Application.DTOs;

public interface IScheduleImportService
{
    Task<ScheduleSyncSummaryDto> ImportScheduleAsync(CancellationToken cancellationToken = default);
}
