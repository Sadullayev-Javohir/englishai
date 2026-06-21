namespace Infrastructure.Video;

/// <summary>
/// Configuration for the <see cref="CookieManager"/>, bound from the "YouTubeCookies" config section.
/// YouTube cookies (a Netscape-format file exported from a signed-in browser) are the most reliable
/// way past YouTube's datacenter "confirm you're not a bot" wall for the cookie-backed yt-dlp fetch.
///
/// A cookie file cannot be re-minted programmatically (that needs a real logged-in browser session),
/// so "refresh" here means rotating across a pool of mounted files and skipping ones that have gone
/// stale - operators replace the files out of band. Both a single <see cref="Path"/> and a
/// <see cref="Directory"/> of files are supported; either, both, or neither may be set (neither =
/// cookie-less, which is fine on a residential dev IP - docs/development-guide.md rule 8).
/// </summary>
public sealed class CookieOptions
{
    public const string SectionName = "YouTubeCookies";

    /// <summary>A single Netscape cookies file. Back-compat alias for the old <c>YtDlp:CookiesPath</c>.</summary>
    public string? Path { get; set; }

    /// <summary>
    /// A directory holding one or more cookie files (<c>*.txt</c>); all are pooled and the freshest
    /// usable one is chosen, so several throwaway accounts can be rotated automatically.
    /// </summary>
    public string? Directory { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Path) || !string.IsNullOrWhiteSpace(Directory);
}
