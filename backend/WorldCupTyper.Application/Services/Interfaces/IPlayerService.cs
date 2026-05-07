using WorldCupTyper.Application.DTOs;

namespace WorldCupTyper.Application.Services.Interfaces;

public interface IPlayerService
{
    Task<IReadOnlyCollection<PlayerDto>> GetPlayersAsync(CancellationToken cancellationToken = default);
    Task<PlayerDto> CreatePlayerAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default);
    Task<PlayerDto> UpdatePlayerAsync(Guid playerId, UpdatePlayerRequest request, CancellationToken cancellationToken = default);
    Task DeactivatePlayerAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<ResetPasswordResponse> ResetPasswordAsync(Guid playerId, ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
