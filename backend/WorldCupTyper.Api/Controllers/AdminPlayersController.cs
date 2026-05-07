using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/players")]
public sealed class AdminPlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public AdminPlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<PlayerDto>>> GetPlayers(CancellationToken cancellationToken)
    {
        return Ok(await _playerService.GetPlayersAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PlayerDto>> CreatePlayer([FromBody] CreatePlayerRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerService.CreatePlayerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPlayers), new { id = player.Id }, player);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlayerDto>> UpdatePlayer(Guid id, [FromBody] UpdatePlayerRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _playerService.UpdatePlayerAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _playerService.DeactivatePlayerAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ResetPasswordResponse>> ResetPassword(Guid id, [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _playerService.ResetPasswordAsync(id, request, cancellationToken));
    }
}
