using WorldCupTyper.ArchiveExporter;

namespace WorldCupTyper.Tests;

public class ArchiveOutputGuardTests
{
    [Fact]
    public void RejectsRepositoryRootAndDescendantsButAcceptsExternalSibling()
    {
        var repo = Path.Combine(Path.GetTempPath(), "archive-repo");
        Assert.Throws<InvalidOperationException>(() => ArchiveOutputGuard.Validate(repo, [repo]));
        Assert.Throws<InvalidOperationException>(() => ArchiveOutputGuard.Validate(Path.Combine(repo, "archive-output"), [repo]));
        ArchiveOutputGuard.Validate(repo + "-backups", [repo]);
    }
}
