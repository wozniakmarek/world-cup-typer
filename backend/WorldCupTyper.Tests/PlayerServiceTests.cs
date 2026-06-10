using FluentAssertions;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class PlayerServiceTests
{
    [Fact]
    public async Task CreatePlayer_ShouldRequirePasswordChange()
    {
        using var dbContext = TestDbContextFactory.Create();
        var service = CreateService(dbContext);

        var player = await service.CreatePlayerAsync(
            new CreatePlayerRequest("new@test.local", "New Player", "Temporary123!", UserRole.Player));

        player.RequiresPasswordChange.Should().BeTrue();
        dbContext.Users.Single().RequiresPasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_ShouldRequirePasswordChangeAgain()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "player@test.local",
            DisplayName = "Player",
            PasswordHash = "old-password",
            Role = UserRole.Player,
            IsActive = true,
            RequiresPasswordChange = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        await service.ResetPasswordAsync(user.Id, new ResetPasswordRequest("Temporary123!"));

        dbContext.Users.Single().RequiresPasswordChange.Should().BeTrue();
    }

    private static PlayerService CreateService(WorldCupTyper.Infrastructure.Persistence.WorldCupTyperDbContext dbContext) =>
        new(dbContext, new TestPasswordHasher(), new TestDateTimeProvider());

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(string passwordHash, string password) => passwordHash == password;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 5, 19, 12, 0, 0, DateTimeKind.Utc);
    }
}
