namespace Infrastructure.Video;

/// <summary>
/// Configuration for the <see cref="YtDlpTranscriptProvider"/>. Bound from the "YtDlp" config
/// section. <see cref="Path"/> is the path to the <c>yt-dlp</c> executable; when unset the
/// provider probes a repo-local <c>tools/yt-dlp</c> (walking up from the app base directory)
/// and finally <c>yt-dlp</c> on the system PATH. <see cref="TimeoutSeconds"/> caps a single
/// caption fetch so a slow/hung call never blocks a lesson read indefinitely.
/// </summary>
public sealed class YtDlpOptions
{
    public const string SectionName = "YtDlp";

    public string? Path { get; set; }

    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// yt-dlp <c>youtube:player_client</c> extractor clients, passed as
    /// <c>--extractor-args "youtube:player_client=&lt;value&gt;"</c>. This is the modern, FREE,
    /// cookie-less lever that gets a datacenter/VPS IP past YouTube's "Sign in to confirm you're not
    /// a bot" wall: the <c>web_safari</c> and innertube clients negotiate the caption surface with a
    /// different bot-check path than the default web client, which on a blocked datacenter IP is the
    /// difference between "every video unavailable" and captions returning normally. Defaults ON
    /// (<c>web_safari,default</c>) so production gets the bypass with no configuration; residential dev
    /// is unaffected (it works either way - verified). The <c>tv</c> client is intentionally excluded
    /// because it does not expose the json3 subtitle format. Set to empty to disable the flag. Override
    /// via <c>YtDlp__PlayerClient</c> if YouTube ever shifts which clients work (docs/development-guide.md rule 8).
    /// </summary>
    public string? PlayerClient { get; set; } = "web_safari,default";

    /// <summary>
    /// Browser to impersonate at the TLS/HTTP layer (e.g. <c>chrome</c>, <c>safari</c>, <c>firefox</c>),
    /// passed to yt-dlp as <c>--impersonate</c>. From a datacenter/VPS IP YouTube blocks the default
    /// client with a "Sign in to confirm you're not a bot" challenge and returns "Video unavailable" for
    /// every video, so the transcript never fills; impersonating a real browser's fingerprint (requires
    /// <c>curl_cffi</c> in the runtime image) defeats that block. Null/empty disables the flag - kept off
    /// in local dev (residential IPs are not blocked and curl_cffi may be absent), enabled in the
    /// container via <c>YtDlp__Impersonate</c> where curl_cffi is installed (docs/development-guide.md rule 8).
    /// </summary>
    public string? Impersonate { get; set; }

    /// <summary>
    /// Path to a Netscape-format cookies file exported from a browser signed in to YouTube, passed to
    /// yt-dlp as <c>--cookies</c>. From a datacenter/VPS IP browser impersonation alone is frequently
    /// NOT enough - YouTube still serves the "Sign in to confirm you're not a bot" challenge and reports
    /// every video as unavailable, so the transcript never fills. Real account cookies are the
    /// documented, most-effective way past that block. Null/empty (or a missing file) disables the flag:
    /// local dev on a residential IP needs no cookies. Set via <c>YtDlp__CookiesPath</c> to a file mounted
    /// into the container; use a throwaway Google account's cookies (docs/development-guide.md rule 8).
    /// </summary>
    public string? CookiesPath { get; set; }
}
