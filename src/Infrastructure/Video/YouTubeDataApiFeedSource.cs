using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
using Application.Video.Models;
using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Official YouTube Data API v3 <see cref="IVideoFeedSource"/>, used when an API key is
/// configured. One <c>search.list</c> call (English-only, strict safe-search, closed-caption,
/// embeddable) yields the page of ids + titles; a single <c>videos.list</c> call resolves
/// their durations, and a batched <c>channels.list</c> lookup supplies channel avatars.
/// The <c>nextPageToken</c> is returned as the continuation. Calls are
/// quota-limited, so the catalog page paginates lazily as the learner scrolls (docs/development-guide.md rule 10).
/// </summary>
public sealed class YouTubeDataApiFeedSource : IVideoFeedSource
{
    private const string SearchBase = "https://www.googleapis.com/youtube/v3/search";
    private const string VideosBase = "https://www.googleapis.com/youtube/v3/videos";
    private const string ChannelsBase = "https://www.googleapis.com/youtube/v3/channels";

    private readonly HttpClient _http;
    private readonly YouTubeOptions _options;

    public YouTubeDataApiFeedSource(HttpClient http, YouTubeOptions options)
    {
        _http = http;
        _options = options;
    }

    public async Task<VideoFeedPage> SearchAsync(
        string searchTerm, string? continuation, int pageSize, CancellationToken cancellationToken)
    {
        var key = Uri.EscapeDataString(_options.ApiKey!);
        var searchUrl =
            $"{SearchBase}?part=snippet&type=video&videoEmbeddable=true&videoCaption=closedCaption" +
            $"&relevanceLanguage=en&safeSearch=strict&maxResults={pageSize}" +
            $"&q={Uri.EscapeDataString(searchTerm)}&key={key}";
        if (!string.IsNullOrWhiteSpace(continuation))
            searchUrl += $"&pageToken={Uri.EscapeDataString(continuation)}";

        using var searchResponse = await _http.GetAsync(searchUrl, cancellationToken);
        searchResponse.EnsureSuccessStatusCode();
        var search = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        var snippets = new List<(string Id, string Title, string Channel, string? ChannelId)>();
        foreach (var item in search.GetProperty("items").EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idObj) || !idObj.TryGetProperty("videoId", out var idEl))
                continue;
            if (idEl.GetString() is not { Length: > 0 } id)
                continue;

            var snippet = item.GetProperty("snippet");
            var title = snippet.GetProperty("title").GetString() ?? id;
            var channel = snippet.GetProperty("channelTitle").GetString() ?? "YouTube";
            var channelId = snippet.TryGetProperty("channelId", out var channelIdEl) ? channelIdEl.GetString() : null;
            snippets.Add((id, title, channel, channelId));
        }

        var durations = await FetchDurationsAsync(snippets.Select(s => s.Id).ToList(), key, cancellationToken);
        var avatars = await FetchChannelAvatarsAsync(
            snippets.Select(s => s.ChannelId).OfType<string>().Distinct().ToArray(), key, cancellationToken);

        var items = snippets
            .Select(s => new VideoFeedResult(
                s.Id, s.Title, s.Channel, durations.GetValueOrDefault(s.Id, 1), HasClosedCaptions: true,
                ChannelAvatarUrl: s.ChannelId is null ? null : avatars.GetValueOrDefault(s.ChannelId)))
            .Where(v => v.DurationSeconds > 0)
            .ToList();

        var next = search.TryGetProperty("nextPageToken", out var tokenEl) ? tokenEl.GetString() : null;
        return new VideoFeedPage(items, string.IsNullOrWhiteSpace(next) ? null : next);
    }

    private async Task<IReadOnlyDictionary<string, string>> FetchChannelAvatarsAsync(
        IReadOnlyList<string> ids, string key, CancellationToken cancellationToken)
    {
        var avatars = new Dictionary<string, string>();
        // One batched channel lookup per result page; the feed decorator caches the enriched page.
        foreach (var batch in ids.Chunk(50))
        {
            var url = $"{ChannelsBase}?part=snippet&id={Uri.EscapeDataString(string.Join(',', batch))}&key={key}";
            try
            {
                using var response = await _http.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                if (!payload.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) continue;
                foreach (var item in items.EnumerateArray())
                {
                    if (!item.TryGetProperty("id", out var idEl) || idEl.GetString() is not { Length: > 0 } id
                        || !item.TryGetProperty("snippet", out var snippet)
                        || !snippet.TryGetProperty("thumbnails", out var thumbnails)) continue;
                    foreach (var size in new[] { "medium", "default", "high" })
                    {
                        if (thumbnails.TryGetProperty(size, out var image)
                            && image.TryGetProperty("url", out var imageUrl)
                            && YouTubeChannelAvatars.Normalize(imageUrl.GetString()) is { } avatar)
                        {
                            avatars[id] = avatar;
                            break;
                        }
                    }
                }
            }
            catch (HttpRequestException) { /* Optional artwork must not make the video feed unavailable. */ }
            catch (JsonException) { /* Keep playable videos if the channel payload is malformed. */ }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
        }
        return avatars;
    }

    private async Task<IReadOnlyDictionary<string, int>> FetchDurationsAsync(
        IReadOnlyList<string> ids, string key, CancellationToken cancellationToken)
    {
        var durations = new Dictionary<string, int>();
        if (ids.Count == 0)
            return durations;

        var url = $"{VideosBase}?part=contentDetails&id={Uri.EscapeDataString(string.Join(',', ids))}&key={key}";
        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        foreach (var item in payload.GetProperty("items").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString();
            var iso = item.GetProperty("contentDetails").GetProperty("duration").GetString() ?? "PT0S";
            if (id is { Length: > 0 })
                durations[id] = Math.Max(1, (int)XmlConvert.ToTimeSpan(iso).TotalSeconds);
        }

        return durations;
    }
}
