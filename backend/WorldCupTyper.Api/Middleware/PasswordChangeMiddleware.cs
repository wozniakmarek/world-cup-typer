using Microsoft.EntityFrameworkCore;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.Abstractions;

namespace WorldCupTyper.Api.Middleware;

public sealed class PasswordChangeMiddleware
{
    private static readonly PathString[] AllowedPaths =
    [
        "/api/auth/me",
        "/api/auth/logout",
        "/api/auth/change-password",
    ];

    private readonly RequestDelegate _next;

    public PasswordChangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAllowedPath(context.Request.Path) || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var dbContext = context.RequestServices.GetRequiredService<IAppDbContext>();
        var userId = context.User.GetUserId();
        var requiresPasswordChange = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => user.RequiresPasswordChange)
            .FirstOrDefaultAsync(context.RequestAborted);

        if (!requiresPasswordChange)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Zmiana hasła jest wymagana przed dalszym korzystaniem z aplikacji.",
            code = "PASSWORD_CHANGE_REQUIRED",
        });
    }

private static bool IsAllowedPath(PathString requestPath)
{
    var normalizedRequestPath = requestPath.Value?.TrimEnd('/') ?? string.Empty;

    return AllowedPaths.Any(path =>
        string.Equals(
            normalizedRequestPath,
            (path.Value ?? string.Empty).TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase));
}
