using System.Net.Http.Json;
using System.Text.Json;
using Application.Video.Models;
using Application.Video.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// Keyless <see cref="IVideoTranscriptProvider"/> over YouTube's public caption surface
/// (PROJECT-SPEC B.3, Bosqich 3). It reads the watch page, then tries the
/// <c>youtubei/v1/get_transcript</c> endpoint and falls back to the <c>timedtext</c>
/// <c>fmt=json3</c> track. Any failure (no captions, or a host whose IP YouTube blocks for
/// caption fetches) returns an empty list rather than throwing, so the lesson keeps its honest
/// "transcript pending" state (docs/development-guide.md rules 8, 11).
///
/// NOTE: caption fetch is blocked from datacenter/sandbox IPs (verified) - this must be
/// confirmed on a production/residential host; the parsing is unit-tested independently.
/// </summary>
public sealed class YouTubeTranscriptProvider : IVideoTranscriptProvider
{
    private const string WatchBase = "https://www.youtube.com/watch?v=";

    private readonly HttpClient _http;
    private readonly ILogger<YouTubeTranscriptProvider> _logger;

    public YouTubeTranscriptProvider(HttpClient http, ILogger<YouTubeTranscriptProvider> logger)
    {
        _http = http;
        _logger = logger;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        }
    }

    public async Task<TranscriptFetchResult> FetchAsync(
        string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        try
        {
            var html = await GetWatchHtmlAsync(youTubeVideoId, cancellationToken);
            if (html is null)
                // The watch page itself was unreachable/blocked - transient, not "no captions".
                return TranscriptFetchResult.ProviderUnavailable;

            var fromApi = await TryGetTranscriptApiAsync(html, cancellationToken);
            if (fromApi.Count > 0)
                return TranscriptFetchResult.Fetched(fromApi);

            var fromTimedText = await TryTimedTextAsync(html, cancellationToken);
            if (fromTimedText.Count > 0)
                return TranscriptFetchResult.Fetched(fromTimedText);

            // We reached YouTube but found no English caption surface for this video - terminal.
            return TranscriptFetchResult.NoCaptions;
        }
        catch (Exception ex)
        {
            // Best-effort enrichment: any failure (blocked IP, an unexpected payload shape) leaves
            // the lesson in its honest "transcript pending" state to retry rather than breaking the
            // read or being made terminal (docs/development-guide.md rules 8, 11). A narrow filter previously let
            // shapes like a string-typed tStartMs surface an InvalidOperationException as a 500.
            _logger.LogWarning(ex, "Transcript fetch failed for video {VideoId}", youTubeVideoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }
    }

    private async Task<string?> GetWatchHtmlAsync(string youTubeVideoId, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(WatchBase + Uri.EscapeDataString(youTubeVideoId), cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync(cancellationToken) : null;
    }

    private async Task<IReadOnlyList<TranscriptLine>> TryGetTranscriptApiAsync(
        string html, CancellationToken cancellationToken)
    {
        var prms = TranscriptParser.ExtractTranscriptParams(html);
        var innertube = YouTubeSearchParser.ExtractInnertube(html);
        if (prms is null || innertube is null)
            return Array.Empty<TranscriptLine>();

        var url = $"https://www.youtube.com/youtubei/v1/get_transcript?key={Uri.EscapeDataString(innertube.Value.ApiKey)}";
        var body = new
        {
            context = new { client = new { clientName = "WEB", clientVersion = innertube.Value.ClientVersion, hl = "en", gl = "US" } },
            @params = prms,
        };

        using var response = await _http.PostAsJsonAsync(url, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<TranscriptLine>();

        using var payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return TranscriptParser.ParseGetTranscript(payload.RootElement);
    }

    private async Task<IReadOnlyList<TranscriptLine>> TryTimedTextAsync(
        string html, CancellationToken cancellationToken)
    {
        var baseUrl = TranscriptParser.ExtractCaptionBaseUrl(html);
        if (baseUrl is null)
            return Array.Empty<TranscriptLine>();

        var url = baseUrl.Contains("fmt=", StringComparison.Ordinal) ? baseUrl : baseUrl + "&fmt=json3";
        using var response = await _http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return Array.Empty<TranscriptLine>();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (stream.CanSeek && stream.Length == 0)
            return Array.Empty<TranscriptLine>();

        using var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return TranscriptParser.ParseTimedTextJson3(payload.RootElement);
    }
}
