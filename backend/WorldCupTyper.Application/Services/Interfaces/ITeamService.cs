using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface ITeamService
{
    Task<IReadOnlyCollection<TeamDto>> GetTeamsAsync(CancellationToken cancellationToken = default);
    Task<TeamDto> CreateTeamAsync(UpsertTeamRequest request, CancellationToken cancellationToken = default);
    Task<TeamDto> UpdateTeamAsync(Guid teamId, UpsertTeamRequest request, CancellationToken cancellationToken = default);
}
