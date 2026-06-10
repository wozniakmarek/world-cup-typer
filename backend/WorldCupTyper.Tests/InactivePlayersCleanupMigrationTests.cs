using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using WorldCupTyper.Infrastructure.Persistence;

namespace WorldCupTyper.Tests;

public sealed class InactivePlayersCleanupMigrationTests
{
    [Fact]
    public void RemoveInactivePlayers_ShouldDeleteOnlyInactiveUsers()
    {
        var migrationType = typeof(WorldCupTyperDbContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => typeof(Migration).IsAssignableFrom(type)
                && type.Name.Contains("RemoveInactivePlayers", StringComparison.Ordinal));

        migrationType.Should().NotBeNull();

        var migration = (Migration)Activator.CreateInstance(migrationType!)!;
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        typeof(Migration)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, [migrationBuilder]);

        var sql = string.Join(
            Environment.NewLine,
            migrationBuilder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        sql.Should().Contain("DELETE FROM \"Users\" AS inactive_user");
        sql.Should().Contain("WHERE NOT inactive_user.\"IsActive\"");
        sql.Should().NotContain("DELETE FROM \"Predictions\"");
        sql.Should().NotContain("DELETE FROM \"LeaderboardSnapshots\"");
    }
}
