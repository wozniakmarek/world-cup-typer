namespace WorldCupTyper.Infrastructure.FootballData;

public interface IFootballDataClient
{
    Task<IReadOnlyCollection<FootballDataMatchSyncModel>> GetCompetitionMatchesAsync(CancellationToken cancellationToken = default);
}
