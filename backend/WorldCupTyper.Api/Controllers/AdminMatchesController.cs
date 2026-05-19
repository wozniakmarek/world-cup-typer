using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/matches")]
public sealed class AdminMatchesController : ControllerBase
{
    private readonly IMatchService _matchService;
    private readonly IMatchSettlementService _matchSettlementService;
    private readonly IScheduleImportService _scheduleImportService;

    public AdminMatchesController(
        IMatchService matchService,
        IMatchSettlementService matchSettlementService,
        IScheduleImportService scheduleImportService)
    {
        _matchService = matchService;
        _matchSettlementService = matchSettlementService;
        _scheduleImportService = scheduleImportService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AdminMatchDto>>> GetMatches(CancellationToken cancellationToken)
    {
        return Ok(await _matchService.GetAdminMatchesAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminMatchDto>> Create([FromBody] UpsertMatchRequest request, CancellationToken cancellationToken)
    {
        var match = await _matchService.CreateMatchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMatches), new { id = match.Id }, match);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminMatchDto>> Update(Guid id, [FromBody] UpsertMatchRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _matchService.UpdateMatchAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/result")]
    public async Task<ActionResult<AdminMatchDto>> SetResult(Guid id, [FromBody] SetMatchResultRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _matchService.SetResultAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/settle")]
    public async Task<IActionResult> Settle(Guid id, CancellationToken cancellationToken)
    {
        await _matchSettlementService.SettleMatchAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("recalculate-ranking")]
    public async Task<IActionResult> RecalculateRanking(CancellationToken cancellationToken)
    {
        await _matchSettlementService.RecalculateRankingsAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("sync-football-data")]
    public async Task<ActionResult<ScheduleSyncSummaryDto>> SyncFootballData(CancellationToken cancellationToken)
    {
        return Ok(await _scheduleImportService.ImportScheduleAsync(cancellationToken));
    }
}
