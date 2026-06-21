using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
using Application.Video.Models;
using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Real YouTube Data API v3 adapter. Fetches a video's title, channel and duration via a
/// single <c>videos?part=snippet,contentDetails</c> call (one API-key request). Captions
/// require OAuth, so the transcript is left for a separate captions/Azure STT step
/// (PROJECT-SPEC B.3); the description seeds the leveler in the meantime. Calls are
/// quota-limited, so ingestion runs from a Hangfire background job (docs/development-guide.md rule 17.3).
/// </summary>
public sealed class YouTubeMetadataProvider : IYouTubeMetadataProvider
{
    private const string ApiBase = "https://www.googleapis.com/youtube/v3/videos";

    private readonly HttpClient _http;
    private readonly YouTubeOptions _options;

    public YouTubeMetadataProvider(HttpClient http, YouTubeOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<VideoMetadata> FetchAsync(string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        var url = $"{ApiBase}?part=snippet,contentDetails&id={Uri.EscapeDataString(youTubeVideoId)}" +
                  $"&key={Uri.EscapeDataString(_options.ApiKey!)}";

        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var items = payload.GetProperty("items");
        if (items.GetArrayLength() == 0)
            throw new InvalidOperationException($"YouTube video '{youTubeVideoId}' was not found.");

        var item = items[0];
        var snippet = item.GetProperty("snippet");
        var title = snippet.GetProperty("title").GetString() ?? youTubeVideoId;
        var channel = snippet.GetProperty("channelTitle").GetString() ?? "YouTube";
        var description = snippet.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty;

        var isoDuration = item.GetProperty("contentDetails").GetProperty("duration").GetString() ?? "PT0S";
        var durationSeconds = Math.Max(1, (int)XmlConvert.ToTimeSpan(isoDuration).TotalSeconds);

        // Until captions/STT are wired, seed the transcript from the description so the
        // leveler has English text to estimate from.
        var transcript = string.IsNullOrWhiteSpace(description)
            ? Array.Empty<TranscriptLine>()
            : new[] { new TranscriptLine(0, durationSeconds, description.Trim()) };

        return new VideoMetadata(youTubeVideoId, title, channel, durationSeconds, transcript);
    }
}
