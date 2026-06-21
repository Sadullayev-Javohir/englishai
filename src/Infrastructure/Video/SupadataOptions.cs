namespace Infrastructure.Video;

/// <summary>
/// Configuration for the <see cref="SupadataTranscriptProvider"/>, bound from the "Supadata" config
/// section. Supadata (api.supadata.ai) is a managed YouTube-transcript REST service: our server only
/// makes an HTTPS call to <em>their</em> API and they fetch captions from their own (unblocked)
/// infrastructure, so the prod VPS's datacenter IP never touches YouTube and the "confirm you're not
/// a bot" wall that defeats a direct yt-dlp/timedtext fetch simply does not apply (see
/// <see cref="SupadataTranscriptProvider"/>). A free account provides an <see cref="ApiKey"/>; with no
/// key the provider is not wired and the app falls back to yt-dlp (docs/development-guide.md rules 8, 10).
/// </summary>
public sealed class SupadataOptions
{
    public const string SectionName = "Supadata";

    /// <summary>
    /// API key from a Supadata account, sent as the <c>x-api-key</c> header. Null/empty leaves the
    /// provider unconfigured. Set via user-secrets in dev and <c>Supadata__ApiKey</c> in prod - never
    /// commit it (docs/development-guide.md rule 13).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Base URL of the Supadata v1 API; overridable only if the vendor ever changes it.</summary>
    public string BaseUrl { get; set; } = "https://api.supadata.ai/v1";

    /// <summary>Caps a single transcript fetch so a slow upstream never blocks a lesson read.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>True when an API key is present, so the provider can be wired (docs/development-guide.md rule 10).</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
