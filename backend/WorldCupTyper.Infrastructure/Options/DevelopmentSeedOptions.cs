namespace WorldCupTyper.Infrastructure.Options;

public sealed class DevelopmentSeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; } = true;
    public string AdminEmail { get; set; } = "admin@marekwozniak.me";
    public string AdminPassword { get; set; } = "ChangeMe123!";
    public string DefaultPlayerPassword { get; set; } = "ChangeMe123!";
}
