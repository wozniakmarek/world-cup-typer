using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface IRankingService
{
    Task<IReadOnlyCollection<LeaderboardEntryDto>> GetRankingAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LeaderboardEntryDto>> GetTopAsync(int count = 5, Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<LeaderboardEntryDto> GetUserRankingAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RankingProgressPointDto>> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RankingProgressSeriesDto>> GetProgressForRankingAsync(Guid? currentUserId = null, CancellationToken cancellationToken = default);
}
