using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Video.Models;
using Application.Video.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// <see cref="IVideoTranscriptProvider"/> backed by the <c>youtube-transcript-service</c> Node sidecar,
/// which fetches captions with <c>youtubei.js</c> (the InnerTube API the YouTube web client uses). This
/// is the FIRST provider in the fallback chain: it is fast and key-less, and on many networks it returns
/// captions where a direct fetch is blocked. The sidecar's three response shapes map 1:1 to
/// <see cref="TranscriptFetchOutcome"/> - <c>found:true</c> → Fetched, <c>found:false</c> → NoCaptions,
/// any non-2xx / network failure → ProviderUnavailable - so a transient sidecar hiccup falls through to
/// yt-dlp/Supadata instead of poisoning the lesson (docs/development-guide.md rule 8). It never throws.
/// </summary>
public sealed class YoutubeiTranscriptProvider : IVideoTranscriptProvider
{
    private readonly HttpClient _http;
    private readonly YoutubeiOptions _options;
    private readonly ILogger<YoutubeiTranscriptProvider> _logger;

    public YoutubeiTranscriptProvider(
        HttpClient http, YoutubeiOptions options, ILogger<YoutubeiTranscriptProvider> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<TranscriptFetchResult> FetchAsync(
        string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
            return TranscriptFetchResult.ProviderUnavailable;

        var url = $"{_options.BaseUrl!.TrimEnd('/')}/transcript";
        try
        {
            using var response = await _http.PostAsJsonAsync(
                url, new { videoId = youTubeVideoId }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "youtubei sidecar returned HTTP {Status} for video {VideoId}; trying the next source.",
                    (int)response.StatusCode, youTubeVideoId);
                return TranscriptFetchResult.ProviderUnavailable;
            }

            var payload = await response.Content.ReadFromJsonAsync<SidecarResponse>(
                cancellationToken: cancellationToken);
            return Interpret(payload, youTubeVideoId);
        }
        catch (Exception ex)
        {
            // Network error, timeout, or an unexpected body: never terminal - fall through so a later
            // provider (or a retry) can still fill the transcript (docs/development-guide.md rule 8).
            _logger.LogWarning(ex, "youtubei sidecar fetch failed for video {VideoId}; will fall through.", youTubeVideoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }
    }

    private TranscriptFetchResult Interpret(SidecarResponse? payload, string videoId)
    {
        if (payload is null)
            return TranscriptFetchResult.ProviderUnavailable;

        if (!payload.Found)
            // The sidecar ran and confirmed the video has no caption track - terminal.
            return TranscriptFetchResult.NoCaptions;

        var lines = (payload.Lines ?? Array.Empty<SidecarLine>())
            .Where(l => !string.IsNullOrWhiteSpace(l.Text))
            .Select(l => new TranscriptLine(
                l.Start, l.End > l.Start ? l.End : l.Start + 1, l.Text!.Trim()))
            .ToList();

        if (lines.Count == 0)
        {
            _logger.LogWarning(
                "youtubei sidecar reported found=true but no usable lines for video {VideoId}; falling through.",
                videoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }

        return TranscriptFetchResult.Fetched(lines);
    }

    private sealed record SidecarResponse(
        [property: JsonPropertyName("found")] bool Found,
        [property: JsonPropertyName("lines")] IReadOnlyList<SidecarLine>? Lines,
        [property: JsonPropertyName("reason")] string? Reason);

    private sealed record SidecarLine(
        [property: JsonPropertyName("start")] double Start,
        [property: JsonPropertyName("end")] double End,
        [property: JsonPropertyName("text")] string? Text);
}
