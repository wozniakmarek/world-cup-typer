using WorldCupTyper.Infrastructure.Options;

namespace WorldCupTyper.Infrastructure.Services;

public sealed record WebPushRequest(string Endpoint, string P256dh, string Auth, string Payload);

public interface IWebPushSender
{
    Task SendAsync(WebPushRequest request, WebPushOptions options, CancellationToken cancellationToken);
}
