using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ranking")]
public sealed class RankingController : ControllerBase
{
    private readonly IRankingService _rankingService;

    public RankingController(IRankingService rankingService)
    {
        _rankingService = rankingService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<LeaderboardEntryDto>>> GetRanking(CancellationToken cancellationToken)
    {
        return Ok(await _rankingService.GetRankingAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("top")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyCollection<LeaderboardEntryDto>>> GetTop(CancellationToken cancellationToken)
    {
        Guid? currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        return Ok(await _rankingService.GetTopAsync(5, currentUserId, cancellationToken));
    }

    [HttpGet("me")]
    public async Task<ActionResult<LeaderboardEntryDto>> GetMe(CancellationToken cancellationToken)
    {
        return Ok(await _rankingService.GetUserRankingAsync(User.GetUserId(), cancellationToken));
    }

    [HttpGet("progress")]
    public async Task<ActionResult<IReadOnlyCollection<RankingProgressPointDto>>> GetProgress(CancellationToken cancellationToken)
    {
        return Ok(await _rankingService.GetProgressAsync(User.GetUserId(), cancellationToken));
    }
}
