using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface IMatchService
{
    Task<IReadOnlyCollection<MatchSummaryDto>> GetMatchesAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MatchSummaryDto>> GetTodayMatchesAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MatchSummaryDto>> GetUpcomingMatchesAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<MatchDetailsDto> GetMatchDetailsAsync(Guid matchId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<AdminMatchDto>> GetAdminMatchesAsync(CancellationToken cancellationToken = default);
    Task<AdminMatchDto> CreateMatchAsync(UpsertMatchRequest request, CancellationToken cancellationToken = default);
    Task<AdminMatchDto> UpdateMatchAsync(Guid matchId, UpsertMatchRequest request, CancellationToken cancellationToken = default);
    Task<AdminMatchDto> SetResultAsync(Guid matchId, SetMatchResultRequest request, CancellationToken cancellationToken = default);
}
