using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class PredictionsController : ControllerBase
{
    private readonly IPredictionService _predictionService;

    public PredictionsController(IPredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    [HttpGet("predictions/my")]
    public async Task<ActionResult<IReadOnlyCollection<MyPredictionDto>>> GetMyPredictions(CancellationToken cancellationToken)
    {
        return Ok(await _predictionService.GetMyPredictionsAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPost("matches/{matchId:guid}/prediction")]
    public async Task<ActionResult<PredictionSummaryDto>> CreatePrediction(Guid matchId, [FromBody] SavePredictionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _predictionService.CreatePredictionAsync(User.GetUserId(), matchId, request, cancellationToken));
    }

    [HttpPut("matches/{matchId:guid}/prediction")]
    public async Task<ActionResult<PredictionSummaryDto>> UpdatePrediction(Guid matchId, [FromBody] SavePredictionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _predictionService.UpdatePredictionAsync(User.GetUserId(), matchId, request, cancellationToken));
    }

    [HttpGet("matches/{matchId:guid}/predictions")]
    public async Task<ActionResult<MatchPredictionsResponseDto>> GetMatchPredictions(Guid matchId, CancellationToken cancellationToken)
    {
        return Ok(await _predictionService.GetPredictionsForMatchAsync(User.GetUserId(), User.IsAdmin(), matchId, cancellationToken));
    }
}
