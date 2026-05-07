namespace WorldCupTyper.Application.Abstractions;

public interface IKnockoutResolverService
{
    Task ResolveNextRoundAsync(Guid completedMatchId, CancellationToken cancellationToken = default);
}
