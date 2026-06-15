using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WorldCupTyper.Api.Extensions;
using WorldCupTyper.Application.Abstractions;
using WorldCupTyper.Application.DTOs;
using WorldCupTyper.Application.Services.Interfaces;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationPreferenceService _preferenceService;
    private readonly INotificationSubscriptionService _subscriptionService;
    private readonly INotificationService _notificationService;
    private readonly WebPushOptions _webPushOptions;

    public NotificationsController(
        INotificationPreferenceService preferenceService,
        INotificationSubscriptionService subscriptionService,
        INotificationService notificationService,
        IOptions<WebPushOptions> webPushOptions)
    {
        _preferenceService = preferenceService;
        _subscriptionService = subscriptionService;
        _notificationService = notificationService;
        _webPushOptions = webPushOptions.Value;
    }

    [HttpGet("settings")]
    public async Task<ActionResult<NotificationSettingsResponse>> GetSettings(CancellationToken cancellationToken)
    {
        return Ok(await _preferenceService.GetSettingsAsync(User.GetUserId(), cancellationToken));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<NotificationSettingsResponse>> UpdateSettings(
        [FromBody] UpdateNotificationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _preferenceService.UpdateSettingsAsync(User.GetUserId(), request, cancellationToken));
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> SaveSubscription([FromBody] SavePushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await _subscriptionService.SaveSubscriptionAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("subscriptions/{id:guid}")]
    public async Task<IActionResult> RevokeSubscription(Guid id, CancellationToken cancellationToken)
    {
        await _subscriptionService.RevokeSubscriptionAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("subscriptions/current")]
    public async Task<IActionResult> RevokeCurrentSubscription([FromBody] RevokePushSubscriptionRequest request, CancellationToken cancellationToken)
    {
        await _subscriptionService.RevokeCurrentSubscriptionAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("test")]
    public async Task<ActionResult<TestNotificationResponse>> SendTestNotification(CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.SendTestNotificationAsync(User.GetUserId(), cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("vapid-public-key")]
    public ActionResult<WebPushPublicKeyResponse> GetVapidPublicKey()
    {
        return Ok(new WebPushPublicKeyResponse(_webPushOptions.PublicKey));
    }
}
