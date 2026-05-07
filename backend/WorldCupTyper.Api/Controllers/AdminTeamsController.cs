using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/teams")]
public sealed class AdminTeamsController : ControllerBase
{
    private readonly ITeamService _teamService;

    public AdminTeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create([FromBody] UpsertTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await _teamService.CreateTeamAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = team.Id }, team);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TeamDto>> Update(Guid id, [FromBody] UpsertTeamRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _teamService.UpdateTeamAsync(id, request, cancellationToken));
    }
}
