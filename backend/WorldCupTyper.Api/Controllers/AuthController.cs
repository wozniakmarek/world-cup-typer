using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<CurrentUserDto>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _authService.ChangePasswordAsync(User.GetUserId(), request, cancellationToken);
        return Ok(user);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var user = await _authService.GetCurrentUserAsync(User.GetUserId(), cancellationToken);
        return Ok(user);
    }

    [Authorize]
    [HttpPut("me/avatar")]
    public async Task<ActionResult<CurrentUserDto>> UpdateAvatar([FromBody] UpdateAvatarRequest request, CancellationToken cancellationToken)
    {
        var user = await _authService.UpdateAvatarAsync(User.GetUserId(), request, cancellationToken);
        return Ok(user);
    }
}
