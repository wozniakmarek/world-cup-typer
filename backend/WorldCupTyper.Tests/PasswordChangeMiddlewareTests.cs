using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WorldCupTyper.Api.Middleware;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Tests.Helpers;

namespace WorldCupTyper.Tests;

public sealed class PasswordChangeMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldBlockProtectedEndpointWhenPasswordChangeIsRequired()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser(requiresPasswordChange: true);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var wasNextCalled = false;
        var middleware = new PasswordChangeMiddleware(_ =>
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext(user.Id, "/api/matches");
        context.RequestServices = new ServiceCollection()
            .AddSingleton<IAppDbContext>(dbContext)
            .BuildServiceProvider();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        wasNextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ShouldAllowChangingPasswordWhenPasswordChangeIsRequired()
    {
        using var dbContext = TestDbContextFactory.Create();
        var user = CreateUser(requiresPasswordChange: true);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var wasNextCalled = false;
        var middleware = new PasswordChangeMiddleware(_ =>
        {
            wasNextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext(user.Id, "/api/auth/change-password");
        context.RequestServices = new ServiceCollection()
            .AddSingleton<IAppDbContext>(dbContext)
            .BuildServiceProvider();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        wasNextCalled.Should().BeTrue();
    }

    private static DefaultHttpContext CreateContext(Guid userId, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                "Test"));

        return context;
    }

    private static ApplicationUser CreateUser(bool requiresPasswordChange) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = "player@test.local",
            DisplayName = "Player",
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            RequiresPasswordChange = requiresPasswordChange,
            CreatedAtUtc = DateTime.UtcNow,
        };
}
