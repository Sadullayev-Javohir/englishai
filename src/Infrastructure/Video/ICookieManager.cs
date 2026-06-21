namespace Infrastructure.Video;

/// <summary>
/// Centralised manager for the YouTube cookie pool used by the cookie-backed transcript fetch. It owns
/// selecting the freshest usable cookie file, judging staleness (Netscape expiry), rotating past a file
/// that has gone stale or was just rejected by YouTube, and reporting health - so no provider has to
/// know where cookies live or whether they are still valid. Implementations are thread-safe: the
/// transcript orchestrator may resolve cookies concurrently for different videos.
/// </summary>
public interface ICookieManager
{
    /// <summary>
    /// The path to the freshest usable cookie file, or <c>null</c> when none are configured or all are
    /// stale (the fetch then proceeds cookie-less). Safe to call on every fetch - it reflects the
    /// current pool, so a file replaced on disk is picked up without a restart.
    /// </summary>
    string? GetActiveCookieFile();

    /// <summary>
    /// Rotates away from the current active file - call after a cookie-backed fetch was still blocked,
    /// so it is skipped and the next usable file (if any) becomes active. Returns <c>true</c> when a
    /// different usable file is now active (worth a retry); <c>false</c> when none remains.
    /// </summary>
    bool Rotate();

    /// <summary>A health snapshot of the cookie pool for diagnostics and logging.</summary>
    CookieStatus GetStatus();
}
