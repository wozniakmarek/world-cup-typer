using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Infrastructure.Persistence;

namespace WorldCupTyper.Tests.Helpers;

internal static class TestDbContextFactory
{
    public static WorldCupTyperDbContext Create()
    {
        var options = new DbContextOptionsBuilder<WorldCupTyperDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WorldCupTyperDbContext(options);
    }
}
