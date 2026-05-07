using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/matches")]
public sealed class MatchesController : ControllerBase
{
    private readonly IMatchService _matchService;

    public MatchesController(IMatchService matchService)
    {
        _matchService = matchService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MatchSummaryDto>>> GetMatches(CancellationToken cancellationToken)
    {
        return Ok(await _matchService.GetMatchesAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("today")]
    public async Task<ActionResult<IReadOnlyCollection<MatchSummaryDto>>> GetToday(CancellationToken cancellationToken)
    {
        return Ok(await _matchService.GetTodayMatchesAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyCollection<MatchSummaryDto>>> GetUpcoming(CancellationToken cancellationToken)
    {
        return Ok(await _matchService.GetUpcomingMatchesAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MatchDetailsDto>> GetMatch(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _matchService.GetMatchDetailsAsync(id, User.GetUserId(), User.IsAdmin(), cancellationToken));
    }
}
