namespace WorldCupTyper.Application.DTOs;

public sealed record ScheduleSyncSummaryDto(
    int ImportedMatches,
    int UpdatedMatches,
    int SkippedMatches,
    int SettledMatches,
    int FailedMatches);
