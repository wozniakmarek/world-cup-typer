namespace WorldCupTyper.Infrastructure.Options;

public sealed class DatabaseStartupOptions
{
    public const string SectionName = "DatabaseStartup";

    public bool ApplyMigrationsOnStartup { get; set; } = true;
}
