namespace Infrastructure.Video;

/// <summary>
/// Resolves the path to the <c>yt-dlp</c> executable used by <see cref="YtDlpTranscriptProvider"/>.
/// Probes, in order: an explicitly configured path, a repo-local <c>tools/yt-dlp</c> found by
/// walking up from the app base directory (so it works regardless of the current directory), and
/// finally the bare name <c>yt-dlp</c> (resolved against the system PATH by the OS).
/// </summary>
internal static class YtDlpExecutable
{
    public static string? Resolve(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return File.Exists(configuredPath) ? configuredPath : null;

        var repoLocal = FindRepoLocal();
        if (repoLocal is not null)
            return repoLocal;

        // Fall back to PATH: the OS resolves the bare command name when the process starts.
        // (We cannot cheaply prove it exists here without spawning, so we trust PATH.)
        return "yt-dlp";
    }

    private static string? FindRepoLocal()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            foreach (var candidate in new[]
                     {
                         Path.Combine(dir.FullName, "tools", "yt-dlp"),
                         Path.Combine(dir.FullName, "tools", "yt-dlp.exe"),
                     })
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
