using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using WorldCupTyper.Api.Controllers;

namespace WorldCupTyper.Tests;

public sealed class RankingControllerAuthorizationTests
{
    [Fact]
    public void GetTop_ShouldAllowAnonymousUsers()
    {
        var method = typeof(RankingController).GetMethod(nameof(RankingController.GetTop));

        typeof(RankingController).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Should().NotBeEmpty();
        method.Should().NotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Should().NotBeEmpty();
    }
}
