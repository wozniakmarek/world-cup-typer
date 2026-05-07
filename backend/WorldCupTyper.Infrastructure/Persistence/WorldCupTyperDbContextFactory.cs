using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WorldCupTyper.Infrastructure.Persistence;

public sealed class WorldCupTyperDbContextFactory : IDesignTimeDbContextFactory<WorldCupTyperDbContext>
{
    public WorldCupTyperDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=world_cup_typer;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<WorldCupTyperDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WorldCupTyperDbContext(optionsBuilder.Options);
    }
}
