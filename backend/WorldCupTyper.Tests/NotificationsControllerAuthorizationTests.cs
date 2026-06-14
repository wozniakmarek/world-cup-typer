using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using WorldCupTyper.Api.Controllers;

namespace WorldCupTyper.Tests;

public sealed class NotificationsControllerAuthorizationTests
{
    [Fact]
    public void NotificationsController_ShouldRequireAuthenticatedUser()
    {
        typeof(NotificationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Should()
            .NotBeEmpty();
    }

    [Fact]
    public void GetVapidPublicKey_ShouldAllowAnonymousAccess()
    {
        var method = typeof(NotificationsController).GetMethod("GetVapidPublicKey");

        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Should().NotBeEmpty();
    }
}
