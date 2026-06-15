using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WorldCupTyper.Infrastructure.Options;

public sealed class WebPushOptionsValidator : IValidateOptions<WebPushOptions>
{
    private const string PartialConfigurationMessage =
        "WebPush options require Subject, PublicKey, and PrivateKey when any WebPush option is configured.";

    private const string InvalidSubjectMessage =
        "WebPush:Subject must be a valid mailto: address or https:// URL.";

    public ValidateOptionsResult Validate(string? name, WebPushOptions options)
    {
        var subject = options.Subject?.Trim() ?? string.Empty;
        var publicKey = RemoveWhitespace(options.PublicKey);
        var privateKey = RemoveWhitespace(options.PrivateKey);

        var configuredValues = new[] { subject, publicKey, privateKey }
            .Count(value => !string.IsNullOrWhiteSpace(value));

        if (configuredValues == 0)
        {
            return ValidateOptionsResult.Success;
        }

        if (configuredValues < 3)
        {
            return ValidateOptionsResult.Fail(PartialConfigurationMessage);
        }

        return IsValidSubject(subject)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(InvalidSubjectMessage);
    }

    private static bool IsValidSubject(string subject)
    {
        if (!Uri.TryCreate(subject, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return true;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var address = subject["mailto:".Length..];
        if (address.Contains('?') || !address.Contains('@'))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(address);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string RemoveWhitespace(string? value) =>
        new((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character)).ToArray());
}
