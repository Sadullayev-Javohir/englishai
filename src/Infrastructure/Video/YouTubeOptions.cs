namespace Infrastructure.Video;

/// <summary>
/// Configuration for the YouTube Data API metadata provider. Bound from the "YouTube"
/// config section. The API key comes from user-secrets/env (docs/development-guide.md rule 13) - never
/// hardcoded. When unset, the deterministic Local provider is used instead.
/// </summary>
public sealed class YouTubeOptions
{
    public const string SectionName = "YouTube";

    public string? ApiKey { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
