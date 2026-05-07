using WorldCupTyper.Domain.Entities;

namespace WorldCupTyper.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateToken(ApplicationUser user);
}
