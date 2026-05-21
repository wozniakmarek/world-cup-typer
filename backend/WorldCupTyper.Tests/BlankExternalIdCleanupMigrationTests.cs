using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using WorldCupTyper.Infrastructure.Persistence;

namespace WorldCupTyper.Tests;

public sealed class BlankExternalIdCleanupMigrationTests
{
    [Fact]
    public void RemoveBlankExternalIdDemoMatches_ShouldDeleteBlankExternalIdMatches()
    {
        var migrationType = typeof(WorldCupTyperDbContext).Assembly
            .GetTypes()
            .SingleOrDefault(type => typeof(Migration).IsAssignableFrom(type)
                && type.Name.Contains("RemoveBlankExternalIdDemoMatches", StringComparison.Ordinal));

        migrationType.Should().NotBeNull();

        var migration = (Migration)Activator.CreateInstance(migrationType!)!;
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        typeof(Migration)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, [migrationBuilder]);

        var sql = string.Join(
            Environment.NewLine,
            migrationBuilder.Operations.OfType<SqlOperation>().Select(operation => operation.Sql));

        sql.Should().Contain("""DELETE FROM "Matches" AS match""");
        sql.Should().Contain("""match."ExternalId" IS NULL OR btrim(match."ExternalId") = ''""");
        sql.Should().Contain("""DELETE FROM "Teams" AS team""");
        sql.Should().Contain("""team."ExternalId" IS NULL OR btrim(team."ExternalId") = ''""");
        sql.Should().NotContain("""match."ExternalId" LIKE 'football-data:%'""");
    }
}
