using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using WorldCupTyper.Domain.Entities;
using WorldCupTyper.Domain.Enums;
using WorldCupTyper.Infrastructure.Auth;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_WithDefaultOptions_ShouldKeepSessionValidForAboutThirtyDays()
    {
        var options = Options.Create(new JwtOptions
        {
            Key = "test-jwt-key-that-is-long-enough-for-hmac",
        });
        var service = new JwtTokenService(options);
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "player@test.local",
            DisplayName = "Player",
            PasswordHash = "hash",
            Role = UserRole.Player,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var before = DateTime.UtcNow;
        var token = service.GenerateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var after = DateTime.UtcNow;

        jwt.ValidTo.Should().BeOnOrAfter(before.AddDays(30).AddMinutes(-1));
        jwt.ValidTo.Should().BeOnOrBefore(after.AddDays(30).AddMinutes(1));
    }
}
