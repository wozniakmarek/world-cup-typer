using FluentAssertions;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Exceptions;
using WorldCupTyper.Application.Services;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_ShouldExposePasswordChangeRequirement()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        user.RequiresPasswordChange = true;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var response = await service.LoginAsync(new LoginRequest(user.Email, "hash"));

        response.User.RequiresPasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_ShouldReplacePasswordAndClearPasswordChangeRequirement()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        user.RequiresPasswordChange = true;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var currentUser = await service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest("hash", "NewPassword123!"));

        currentUser.RequiresPasswordChange.Should().BeFalse();
        dbContext.Users.Single().PasswordHash.Should().Be("NewPassword123!");
        dbContext.Users.Single().RequiresPasswordChange.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_ShouldRejectCurrentPasswordMismatch()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        user.RequiresPasswordChange = true;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var act = () => service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest("wrong", "NewPassword123!"));

        await act.Should().ThrowAsync<UnauthorizedAppException>();
        dbContext.Users.Single().RequiresPasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_ShouldRejectReusingCurrentPassword()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        user.RequiresPasswordChange = true;
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var act = () => service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest("hash", "hash"));

        await act.Should().ThrowAsync<BusinessRuleException>();
        dbContext.Users.Single().RequiresPasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAvatar_ShouldStoreTrimmedAvatarUrl()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var currentUser = await service.UpdateAvatarAsync(
            user.Id,
            new UpdateAvatarRequest("  https://cdn.example.com/avatar.png  "));

        currentUser.AvatarUrl.Should().Be("https://cdn.example.com/avatar.png");
        dbContext.Users.Single().AvatarUrl.Should().Be("https://cdn.example.com/avatar.png");
    }

    [Fact]
    public async Task UpdateAvatar_ShouldClearAvatarUrlWhenBlank()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        user.AvatarUrl = "https://cdn.example.com/avatar.png";
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var currentUser = await service.UpdateAvatarAsync(user.Id, new UpdateAvatarRequest(" "));

        currentUser.AvatarUrl.Should().BeNull();
        dbContext.Users.Single().AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAvatar_ShouldRejectNonHttpAvatarUrl()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var act = () => service.UpdateAvatarAsync(user.Id, new UpdateAvatarRequest("javascript:alert(1)"));

        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    private static AuthService CreateService(WorldCupTyper.Infrastructure.Persistence.WorldCupTyperDbContext dbContext) =>
        new(dbContext, new TestPasswordHasher(), new TestJwtTokenService(), new TestDateTimeProvider());

    private static ApplicationUser CreateUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = "marek@test.local",
            DisplayName = "Marek",
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => password;

        public bool Verify(string passwordHash, string password) => passwordHash == password;
    }

    private sealed class TestJwtTokenService : IJwtTokenService
    {
        public string GenerateToken(ApplicationUser user) => $"token-{user.Id}";
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 5, 19, 12, 0, 0, DateTimeKind.Utc);
    }
}
