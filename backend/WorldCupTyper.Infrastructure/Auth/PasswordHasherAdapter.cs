using Microsoft.AspNetCore.Identity;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Infrastructure.Auth;

public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(new ApplicationUser(), password);
    }

    public bool Verify(string passwordHash, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(new ApplicationUser(), passwordHash, password);
        return result != PasswordVerificationResult.Failed;
    }
}
