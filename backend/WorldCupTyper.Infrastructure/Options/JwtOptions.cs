namespace WorldCupTyper.Infrastructure.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int DefaultExpiryMinutes = 43_200;

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "WorldCupTyper";
    public string Audience { get; set; } = "WorldCupTyper.Client";
    public int ExpiryMinutes { get; set; } = DefaultExpiryMinutes;
}
