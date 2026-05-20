using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class FootballDataPersistenceTests
{
    [Theory]
    [InlineData(typeof(Match))]
    [InlineData(typeof(Team))]
    public void ExternalIdIndexes_ShouldIgnoreNullAndBlankValues(Type entityType)
    {
        using var dbContext = TestDbContextFactory.Create();

        var index = dbContext.Model.FindEntityType(entityType)!
            .GetIndexes()
            .Single(candidate => candidate.Properties.Single().Name == nameof(Match.ExternalId));

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("\"ExternalId\" IS NOT NULL AND btrim(\"ExternalId\") <> ''");
    }

    [Fact]
    public async Task Team_ShouldPersistExternalId()
    {
        using var dbContext = TestDbContextFactory.Create();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            Name = "Poland",
            ShortName = "POL",
            CountryCode = "POL",
            ExternalId = "football-data:794",
        };

        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();
        var saved = dbContext.Teams.Single(candidate => candidate.Id == team.Id);
        saved.ExternalId.Should().Be("football-data:794");
    }
}
