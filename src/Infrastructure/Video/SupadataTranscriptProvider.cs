using System.Net;
using System.Text.Json;
using Application.Video.Models;
using Application.Video.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// <see cref="IVideoTranscriptProvider"/> backed by the Supadata YouTube-transcript REST API
/// (PROJECT-SPEC B.3, Bosqich 3). It GETs <c>{BaseUrl}/youtube/transcript?videoId=…</c> with the
/// account's <c>x-api-key</c> and parses the returned <c>content</c> chunks with
/// <see cref="TranscriptParser.ParseSupadataTranscript"/>, producing the same per-word karaoke
/// transcript a native YouTube fetch would.
///
/// WHY this exists: from the prod VPS's datacenter IP YouTube serves a "confirm you're not a bot"
/// challenge, so a direct yt-dlp/timedtext fetch returns nothing and a lesson's transcript never
/// loads. Supadata fetches captions from its own (unblocked) infrastructure, so our server only ever
/// calls Supadata over HTTPS - the datacenter IP never touches YouTube and the bot wall does not
/// apply. Resilient (docs/development-guide.md rule 8): only an explicit "this video has no transcript" is terminal
/// (<see cref="TranscriptFetchOutcome.NoCaptions"/>); every other failure (missing/invalid key, rate
/// limit, async job, network, unexpected body) is reported as
/// <see cref="TranscriptFetchOutcome.ProviderUnavailable"/> so the fill retries instead of permanently
/// poisoning an otherwise-captioned lesson. It never throws.
/// </summary>
public sealed class SupadataTranscriptProvider : IVideoTranscriptProvider
{
    private readonly HttpClient _http;
    private readonly SupadataOptions _options;
    private readonly ILogger<SupadataTranscriptProvider> _logger;

    public SupadataTranscriptProvider(
        HttpClient http, SupadataOptions options, ILogger<SupadataTranscriptProvider> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<TranscriptFetchResult> FetchAsync(
        string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return TranscriptFetchResult.ProviderUnavailable;

        var url = $"{_options.BaseUrl.TrimEnd('/')}/youtube/transcript" +
                  $"?lang=en&videoId={Uri.EscapeDataString(youTubeVideoId)}";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("x-api-key", _options.ApiKey);

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return response.IsSuccessStatusCode
                ? ParseSuccess(body, youTubeVideoId)
                : ClassifyError(response.StatusCode, body, youTubeVideoId);
        }
        catch (Exception ex)
        {
            // Network error, timeout, or anything unexpected: never terminal - leave the lesson pending
            // so a later open retries instead of settling on "no transcript" (docs/development-guide.md rule 8).
            _logger.LogWarning(ex, "Supadata transcript fetch failed for video {VideoId}; will retry.", youTubeVideoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }
    }

    /// <summary>
    /// Parses a 2xx Supadata body. A <c>content</c> array with lines is the success case; a large
    /// transcript returned as an async <c>jobId</c> (which we do not poll) and an empty/unparseable
    /// body are treated as transient so the fill retries rather than poisoning the lesson (rule 8).
    /// </summary>
    private TranscriptFetchResult ParseSuccess(string body, string videoId)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Supadata returns very large transcripts as an async job instead of inline content. We
            // don't poll the job, so treat it as a transient miss (retry next open), not terminal.
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("jobId", out _) &&
                !root.TryGetProperty("content", out _))
            {
                _logger.LogInformation(
                    "Supadata returned an async job for video {VideoId}; leaving transcript pending.", videoId);
                return TranscriptFetchResult.ProviderUnavailable;
            }

            var lines = TranscriptParser.ParseSupadataTranscript(root);
            if (lines.Count > 0)
                return TranscriptFetchResult.Fetched(lines);

            // Supadata ran and returned an empty transcript: the video has no usable caption track.
            return TranscriptFetchResult.NoCaptions;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Supadata returned an unparseable body for video {VideoId}; will retry.", videoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }
    }

    /// <summary>
    /// Maps a non-2xx Supadata response to an outcome. Only an explicit "this video has no transcript"
    /// error code is terminal; every other status (missing/invalid key, rate limit, quota, server
    /// error) is transient, so a single hiccup never permanently poisons an otherwise-captioned lesson
    /// into "no transcript" forever (docs/development-guide.md rule 8).
    /// </summary>
    private TranscriptFetchResult ClassifyError(HttpStatusCode status, string body, string videoId)
    {
        var code = TryReadErrorCode(body);

        if (IndicatesNoTranscript(code))
            return TranscriptFetchResult.NoCaptions;

        _logger.LogWarning(
            "Supadata transcript fetch for video {VideoId} failed with HTTP {Status} (code: {Code}); will retry.",
            videoId, (int)status, code ?? "<none>");
        return TranscriptFetchResult.ProviderUnavailable;
    }

    /// <summary>
    /// True only when the error code clearly says the video has no transcript (e.g.
    /// <c>transcript-unavailable</c>, <c>no-captions</c>, <c>transcript-not-found</c>,
    /// <c>captions-disabled</c>). Anything else is left transient to avoid poisoning (rule 8).
    /// </summary>
    private static bool IndicatesNoTranscript(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        var mentionsTranscript =
            code.Contains("transcript", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("caption", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("subtitle", StringComparison.OrdinalIgnoreCase);
        if (!mentionsTranscript)
            return false;

        return code.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("not-found", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("disabled", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("no-", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("missing", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Reads Supadata's machine error code (or message) from a JSON error body, if any.</summary>
    private static string? TryReadErrorCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;
            if (root.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String)
                return e.GetString();
            if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                return m.GetString();
        }
        catch (JsonException)
        {
            // A non-JSON error body carries no machine code; treat as transient (handled by caller).
        }
        return null;
    }
}
