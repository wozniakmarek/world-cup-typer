using System.Security.Claims;
using WorldCupTyper.Application.Exceptions;

namespace WorldCupTyper.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId))
        {
            throw new UnauthorizedAppException("Brak poprawnego identyfikatora użytkownika w tokenie.");
        }

        return userId;
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin");
    }
}
