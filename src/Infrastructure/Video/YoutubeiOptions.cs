namespace Infrastructure.Video;

/// <summary>
/// Configuration for the <see cref="YoutubeiTranscriptProvider"/>, bound from the "Youtubei" config
/// section. <see cref="BaseUrl"/> is the address of the youtubei.js sidecar
/// (<c>youtube-transcript-service</c>); when unset the provider is disabled and the orchestrator
/// simply skips it (docs/development-guide.md rule 8 - config-gated, no key in code). <see cref="TimeoutSeconds"/>
/// caps a single sidecar call so a slow/hung fetch never blocks the chain.
/// </summary>
public sealed class YoutubeiOptions
{
    public const string SectionName = "Youtubei";

    /// <summary>Base URL of the youtubei.js sidecar, e.g. <c>http://transcript-service:8080</c>.</summary>
    public string? BaseUrl { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl);
}
