namespace Infrastructure.Images;

/// <summary>
/// Configuration for the licensed image providers (Unsplash and/or Pexels). Bound from the "Images"
/// config section. The access keys come from user-secrets/env (docs/development-guide.md rule 13) - never hardcoded.
/// When neither is set, the local no-op image service is used and the UI falls back to a generated
/// placeholder. Both may be set: the composite service uses Unsplash first and tops up a topic's
/// gallery from Pexels, so a gallery still fills if one provider rate-limits or has few matches.
/// </summary>
public sealed class ImageOptions
{
    public const string SectionName = "Images";

    /// <summary>Unsplash access key (Client-ID). If empty, Unsplash lookups are disabled.</summary>
    public string? UnsplashAccessKey { get; set; }

    /// <summary>Pexels API key. If empty, Pexels lookups are disabled.</summary>
    public string? PexelsApiKey { get; set; }

    public bool HasUnsplash => !string.IsNullOrWhiteSpace(UnsplashAccessKey);
    public bool HasPexels => !string.IsNullOrWhiteSpace(PexelsApiKey);

    public bool IsConfigured => HasUnsplash || HasPexels;
}
