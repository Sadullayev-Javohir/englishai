using Application.Video.Models;
using Application.Video.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// Wraps the cookie-backed transcript provider with the "if the cookie is stale, refresh and retry"
/// step (the orchestrator's step 4). It runs the inner provider with the current active cookie; if that
/// comes back <see cref="TranscriptFetchOutcome.ProviderUnavailable"/> - the shape a YouTube bot-wall
/// rejection takes - it asks the <see cref="ICookieManager"/> to rotate to the next usable cookie and
/// retries the inner provider once. A genuine <see cref="TranscriptFetchOutcome.NoCaptions"/> or a real
/// transcript is returned as-is (no pointless rotation). This keeps cookie rotation out of every other
/// provider and is itself just another <see cref="IVideoTranscriptProvider"/>, so the orchestrator
/// treats it uniformly (docs/development-guide.md rules 8, and "new provider = new class").
/// </summary>
public sealed class CookieRefreshingTranscriptProvider : IVideoTranscriptProvider
{
    private readonly IVideoTranscriptProvider _inner;
    private readonly ICookieManager _cookies;
    private readonly ILogger<CookieRefreshingTranscriptProvider> _logger;

    public CookieRefreshingTranscriptProvider(
        IVideoTranscriptProvider inner,
        ICookieManager cookies,
        ILogger<CookieRefreshingTranscriptProvider> logger)
    {
        _inner = inner;
        _cookies = cookies;
        _logger = logger;
    }

    public async Task<TranscriptFetchResult> FetchAsync(
        string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        var first = await _inner.FetchAsync(youTubeVideoId, cancellationToken);
        if (first.Outcome != TranscriptFetchOutcome.ProviderUnavailable)
            return first;

        // The cookie-backed attempt could not run / was blocked. If a stale cookie is the likely cause,
        // rotate to the next pooled file and try exactly once more before falling through the chain.
        if (!_cookies.Rotate())
            return first;

        _logger.LogInformation(
            "Cookie-backed transcript fetch for video {VideoId} was blocked; rotated cookies and retrying once.",
            youTubeVideoId);
        return await _inner.FetchAsync(youTubeVideoId, cancellationToken);
    }
}
