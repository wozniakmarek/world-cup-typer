using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/summary")]
public sealed class SummaryController : ControllerBase
{
    private readonly IFinalSummaryService _finalSummaryService;

    public SummaryController(IFinalSummaryService finalSummaryService)
    {
        _finalSummaryService = finalSummaryService;
    }

    [HttpGet("final")]
    [AllowAnonymous]
    public async Task<ActionResult<FinalSummaryResponseDto>> GetFinalSummary(CancellationToken cancellationToken)
    {
        Guid? currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : null;
        return Ok(await _finalSummaryService.GetFinalSummaryAsync(currentUserId, cancellationToken));
    }

    [HttpGet("final/me")]
    public async Task<ActionResult<PersonalFinalSummaryResponseDto>> GetMyFinalSummary(CancellationToken cancellationToken)
    {
        return Ok(await _finalSummaryService.GetPersonalFinalSummaryAsync(User.GetUserId(), cancellationToken));
    }
}
