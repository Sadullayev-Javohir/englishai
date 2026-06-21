namespace Infrastructure.Video;

/// <summary>
/// A snapshot of the YouTube cookie pool's health, returned by <see cref="ICookieManager.GetStatus"/>
/// for diagnostics (the admin transcript-health probe and logs). It distinguishes "no cookies
/// configured" (fine on a residential IP) from "configured but every file is stale" (the case an
/// operator must act on by replacing the mounted files).
/// </summary>
public sealed record CookieStatus(
    bool Configured,
    int TotalFiles,
    int UsableFiles,
    string? ActiveFile,
    DateTimeOffset? ActiveExpiresAtUtc)
{
    public static readonly CookieStatus NotConfigured = new(false, 0, 0, null, null);

    /// <summary>True when cookies are configured but none are currently usable - operators must refresh.</summary>
    public bool NeedsRefresh => Configured && UsableFiles == 0;
}
