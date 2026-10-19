namespace WorldCupTyper.ArchiveExporter;

public static class ArchiveOutputGuard
{
    public static void Validate(string output, IEnumerable<string> roots)
    {
        var target = Path.GetFullPath(output).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (var root in roots)
        {
            var repository = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (target.Equals(repository, comparison) || target.StartsWith(repository + Path.DirectorySeparatorChar, comparison))
                throw new InvalidOperationException("Production archives must be written outside every repository/worktree.");
        }
    }

    public static IEnumerable<string> FindRepositories(params string[] starts)
    {
        foreach (var start in starts)
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
                {
                    yield return directory.FullName;
                    break;
                }
    }
}
