using FluentAssertions;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class PlayerServiceTests
{
    [Fact]
    public async Task GetPlayers_ShouldReturnOnlyActivePlayers()
    {
        using var dbContext = TestDbContextFactory.Create();
        dbContext.Users.AddRange(
            CreateUser("admin@test.local", "Admin", UserRole.Admin, isActive: true),
            CreateUser("inactive@test.local", "Inactive", UserRole.Player, isActive: false),
            CreateUser("active@test.local", "Active", UserRole.Player, isActive: true));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var players = await service.GetPlayersAsync();

        players.Select(player => player.DisplayName).Should().Equal("Admin", "Active");
        players.Should().OnlyContain(player => player.IsActive);
    }

    private static PlayerService CreateService(WorldCupTyper.Infrastructure.Persistence.WorldCupTyperDbContext dbContext) =>
        new(dbContext, new TestPasswordHasher(), new TestDateTimeProvider());

    private static ApplicationUser CreateUser(string email, string displayName, UserRole role, bool isActive) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            PasswordHash = "hash",
            Role = role,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(string passwordHash, string password) => passwordHash == password;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
    }
}
