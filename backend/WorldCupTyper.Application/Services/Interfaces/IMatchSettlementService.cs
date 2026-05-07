namespace WorldCupTyper.Application.Services.Interfaces;

public interface IMatchSettlementService
{
    Task SettleMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task RecalculateRankingsAsync(CancellationToken cancellationToken = default);
}
