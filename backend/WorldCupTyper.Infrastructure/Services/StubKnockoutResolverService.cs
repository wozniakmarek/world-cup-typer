using WorldCupTyper.Application.Abstractions;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class StubKnockoutResolverService : IKnockoutResolverService
{
    public Task ResolveNextRoundAsync(Guid completedMatchId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
