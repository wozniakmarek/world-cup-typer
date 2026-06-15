using WebPush;
using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Infrastructure.Services;

public sealed class WebPushSender : IWebPushSender
{
    private readonly WebPushClient _client = new();

    public Task SendAsync(WebPushRequest request, WebPushOptions options, CancellationToken cancellationToken)
    {
        var subscription = new PushSubscription(request.Endpoint, request.P256dh, request.Auth);
        var vapidDetails = new VapidDetails(options.Subject, options.PublicKey, options.PrivateKey);

        return _client.SendNotificationAsync(subscription, request.Payload, vapidDetails, cancellationToken);
    }
}
