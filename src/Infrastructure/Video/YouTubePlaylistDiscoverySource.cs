using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml;
using Application.Video.Models;
using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Fresh long-form discovery for the video catalog. It uses the official Data API when
/// configured and falls back to YouTube's public result/playlist pages in local development.
/// Results are duration-checked before they can be labelled as a full film or episode collection.
/// </summary>
public sealed class YouTubePlaylistDiscoverySource : IVideoPlaylistDiscoverySource
{
    private const string SearchBase = "https://www.googleapis.com/youtube/v3/search";
    private const string PlaylistItemsBase = "https://www.googleapis.com/youtube/v3/playlistItems";
    private const string PlaylistsBase = "https://www.googleapis.com/youtube/v3/playlists";
    private const string VideosBase = "https://www.googleapis.com/youtube/v3/videos";
    private readonly HttpClient _http;
    private readonly YouTubeOptions _options;

    public YouTubePlaylistDiscoverySource(HttpClient http, YouTubeOptions options)
    {
        _http = http;
        _options = options;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/120 Safari/537.36");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        }
    }

    public async Task<IReadOnlyList<VideoPlaylist>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<VideoPlaylist>();
        return _options.IsConfigured
            ? await SearchOfficialAsync(query, maxResults, cancellationToken)
            : await SearchLocalAsync(query, maxResults, cancellationToken);
    }

    public async Task<VideoPlaylist?> GetAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playlistId) || playlistId.Length > 200) return null;
        return _options.IsConfigured
            ? await GetOfficialAsync(playlistId, cancellationToken)
            : await GetLocalAsync(playlistId, cancellationToken);
    }

    private async Task<IReadOnlyList<VideoPlaylist>> SearchOfficialAsync(string query, int maxResults, CancellationToken ct)
    {
        var key = Uri.EscapeDataString(_options.ApiKey!);
        var url = $"{SearchBase}?part=snippet&type=playlist&relevanceLanguage=en&safeSearch=strict&maxResults={Math.Clamp(maxResults, 1, 12)}&q={Uri.EscapeDataString(query + " full movie full episodes")}&key={key}";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!payload.TryGetProperty("items", out var entries) || entries.ValueKind != JsonValueKind.Array) return Array.Empty<VideoPlaylist>();

        var found = new List<VideoPlaylist>();
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("id", out var idNode) || !idNode.TryGetProperty("playlistId", out var idEl) || idEl.GetString() is not { Length: > 0 } id) continue;
            var playlist = await GetOfficialAsync(id, ct);
            if (playlist is not null) found.Add(playlist);
        }
        return found;
    }

    private async Task<VideoPlaylist?> GetOfficialAsync(string playlistId, CancellationToken ct)
    {
        var key = Uri.EscapeDataString(_options.ApiKey!);
        var metaUrl = $"{PlaylistsBase}?part=snippet&id={Uri.EscapeDataString(playlistId)}&key={key}";
        using var metaResponse = await _http.GetAsync(metaUrl, ct);
        metaResponse.EnsureSuccessStatusCode();
        var metaPayload = await metaResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!metaPayload.TryGetProperty("items", out var metaItems) || metaItems.GetArrayLength() == 0) return null;
        var meta = metaItems[0].GetProperty("snippet");
        var title = meta.GetProperty("title").GetString() ?? "YouTube playlist";
        var channel = meta.TryGetProperty("channelTitle", out var channelEl) ? channelEl.GetString() ?? "YouTube" : "YouTube";
        var image = ReadThumbnail(meta);

        var pageUrl = $"{PlaylistItemsBase}?part=snippet,contentDetails&playlistId={Uri.EscapeDataString(playlistId)}&maxResults=24&key={key}";
        using var pageResponse = await _http.GetAsync(pageUrl, ct);
        pageResponse.EnsureSuccessStatusCode();
        var pagePayload = await pageResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!pagePayload.TryGetProperty("items", out var entries) || entries.ValueKind != JsonValueKind.Array) return null;
        var partial = new List<(string VideoId, string Title, string Channel, string? Thumbnail)>();
        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("contentDetails", out var details) || !details.TryGetProperty("videoId", out var videoIdEl) || videoIdEl.GetString() is not { Length: > 0 } videoId) continue;
            var snippet = entry.GetProperty("snippet");
            partial.Add((videoId, snippet.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? videoId : videoId,
                snippet.TryGetProperty("videoOwnerChannelTitle", out var ownerEl) ? ownerEl.GetString() ?? channel : channel, ReadThumbnail(snippet)));
        }
        var durations = await FetchDurationsAsync(partial.Select(item => item.VideoId).ToArray(), key, ct);
        var items = partial.Where(item => durations.GetValueOrDefault(item.VideoId) > 0)
            .Select(item => new VideoPlaylistItem(item.VideoId, item.Title, item.Channel, durations[item.VideoId], item.Thumbnail ?? $"https://i.ytimg.com/vi/{item.VideoId}/hqdefault.jpg"))
            .ToList();
        return IsLongForm(title, items) ? new VideoPlaylist(playlistId, title, channel, image, items) : null;
    }

    private async Task<IReadOnlyList<VideoPlaylist>> SearchLocalAsync(string query, int maxResults, CancellationToken ct)
    {
        var url = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query + " full movie full episodes")}";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        using var document = YouTubeSearchParser.ExtractInitialData(html);
        if (document is null) return Array.Empty<VideoPlaylist>();
        // Search can rank non-English dubs or unavailable playlists first. Inspect a modest
        // wider candidate set, then return only the requested number of duration-checked results.
        var requested = Math.Clamp(maxResults, 1, 12);
        var candidates = YouTubePlaylistParser.CollectPlaylists(document.RootElement).Take(requested * 4).ToList();
        var playlists = new List<VideoPlaylist>();
        foreach (var candidate in candidates)
        {
            var playlist = await GetLocalAsync(candidate.Id, ct);
            if (playlist is not null) playlists.Add(playlist);
            if (playlists.Count >= requested) break;
        }
        return playlists;
    }

    private async Task<VideoPlaylist?> GetLocalAsync(string playlistId, CancellationToken ct)
    {
        var url = $"https://www.youtube.com/playlist?list={Uri.EscapeDataString(playlistId)}";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);
        using var document = YouTubeSearchParser.ExtractInitialData(html);
        if (document is null) return null;
        var playlist = YouTubePlaylistParser.ReadPlaylist(document.RootElement, playlistId);
        return playlist is not null && IsLongForm(playlist.Title, playlist.Items) ? playlist : null;
    }

    private static bool IsLongForm(string title, IReadOnlyList<VideoPlaylistItem> items)
    {
        if (items.Count == 0) return false;
        if (!LooksSuitableForEnglishLearning(title)) return false;
        var total = items.Sum(item => item.DurationSeconds);
        var explicitFull = title.Contains("full movie", StringComparison.OrdinalIgnoreCase)
            || title.Contains("full film", StringComparison.OrdinalIgnoreCase)
            || title.Contains("full episodes", StringComparison.OrdinalIgnoreCase)
            || title.Contains("complete episodes", StringComparison.OrdinalIgnoreCase);
        return total >= TimeSpan.FromMinutes(20).TotalSeconds && (items.Count >= 2 || explicitFull || total >= TimeSpan.FromMinutes(45).TotalSeconds);
    }

    // Search language is English, but YouTube can still surface explicitly dubbed uploads.
    // Do not present them as an English-learning full film/episode collection.
    private static bool LooksSuitableForEnglishLearning(string title) =>
        !new[]
            {
                "hindi", "urdu", "tamil", "telugu", "bengali", "bangla", "malayalam", "dubbed",
                "nursery rhyme", "baby songs", "toddler", "cocomelon", "little baby bum", "for babies",
            }
            .Any(term => title.Contains(term, StringComparison.OrdinalIgnoreCase));

    private async Task<IReadOnlyDictionary<string, int>> FetchDurationsAsync(IReadOnlyList<string> ids, string key, CancellationToken ct)
    {
        if (ids.Count == 0) return new Dictionary<string, int>();
        var url = $"{VideosBase}?part=contentDetails&id={Uri.EscapeDataString(string.Join(',', ids))}&key={key}";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var durations = new Dictionary<string, int>();
        foreach (var item in payload.GetProperty("items").EnumerateArray())
        {
            var id = item.GetProperty("id").GetString();
            var duration = item.GetProperty("contentDetails").GetProperty("duration").GetString() ?? "PT0S";
            if (id is { Length: > 0 }) durations[id] = Math.Max(0, (int)XmlConvert.ToTimeSpan(duration).TotalSeconds);
        }
        return durations;
    }

    private static string? ReadThumbnail(JsonElement snippet)
    {
        if (!snippet.TryGetProperty("thumbnails", out var thumbnails)) return null;
        foreach (var size in new[] { "high", "medium", "default" })
            if (thumbnails.TryGetProperty(size, out var image) && image.TryGetProperty("url", out var url) && url.GetString() is { Length: > 0 } value) return value;
        return null;
    }
}

internal static class YouTubePlaylistParser
{
    internal sealed record Candidate(string Id, string Title, string Channel, string? Thumbnail);

    public static IReadOnlyList<Candidate> CollectPlaylists(JsonElement root)
    {
        var results = new List<Candidate>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Visit(root, results, seen);
        return results;
    }

    public static VideoPlaylist? ReadPlaylist(JsonElement root, string playlistId)
    {
        var items = new List<VideoPlaylistItem>();
        VisitItems(root, items);
        if (items.Count == 0) return null;
        var title = FindText(root, "playlistHeaderRenderer", "title")
            ?? FindText(root, "playlistMetadataRenderer", "title")
            ?? FindText(root, "metadata", "title")
            ?? "YouTube playlist";
        var channel = FindText(root, "playlistHeaderRenderer", "ownerText")
            ?? items.FirstOrDefault()?.Channel
            ?? "YouTube";
        var image = items.FirstOrDefault()?.ThumbnailUrl;
        return new VideoPlaylist(playlistId, title, channel, image, items);
    }

    private static void Visit(JsonElement node, ICollection<Candidate> results, ISet<string> seen)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("playlistRenderer", out var renderer) && TryPlaylist(renderer, out var candidate) && seen.Add(candidate!.Id)) results.Add(candidate);
            if (node.TryGetProperty("lockupViewModel", out var lockup) && TryLockupPlaylist(lockup, out candidate) && seen.Add(candidate!.Id)) results.Add(candidate);
            foreach (var property in node.EnumerateObject()) Visit(property.Value, results, seen);
        }
        else if (node.ValueKind == JsonValueKind.Array) foreach (var item in node.EnumerateArray()) Visit(item, results, seen);
    }

    private static void VisitItems(JsonElement node, ICollection<VideoPlaylistItem> results)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("playlistVideoRenderer", out var renderer) && TryItem(renderer, out var item)) results.Add(item!);
            if (node.TryGetProperty("lockupViewModel", out var lockup) && TryLockupItem(lockup, out item)) results.Add(item!);
            foreach (var property in node.EnumerateObject()) VisitItems(property.Value, results);
        }
        else if (node.ValueKind == JsonValueKind.Array) foreach (var item in node.EnumerateArray()) VisitItems(item, results);
    }

    private static bool TryPlaylist(JsonElement node, out Candidate? candidate)
    {
        candidate = null;
        if (!node.TryGetProperty("playlistId", out var idEl) || idEl.GetString() is not { Length: > 0 } id) return false;
        var title = Text(node, "title");
        if (string.IsNullOrWhiteSpace(title)) return false;
        candidate = new Candidate(id, title, Text(node, "longBylineText") ?? Text(node, "shortBylineText") ?? "YouTube", Thumbnail(node));
        return true;
    }

    private static bool TryLockupPlaylist(JsonElement node, out Candidate? candidate)
    {
        candidate = null;
        if (!string.Equals(Text(node, "contentType"), "LOCKUP_CONTENT_TYPE_PLAYLIST", StringComparison.Ordinal)
            || !TryWatchEndpoint(node, out _, out var playlistId)
            || string.IsNullOrWhiteSpace(playlistId))
            return false;

        var metadata = LockupMetadata(node);
        var title = metadata.ValueKind == JsonValueKind.Object ? Text(metadata, "title") : null;
        if (string.IsNullOrWhiteSpace(title)) return false;

        candidate = new Candidate(
            playlistId,
            title,
            LockupChannel(metadata) ?? "YouTube",
            LockupThumbnail(node));
        return true;
    }

    private static bool TryItem(JsonElement node, out VideoPlaylistItem? item)
    {
        item = null;
        if (!node.TryGetProperty("videoId", out var idEl) || idEl.GetString() is not { Length: > 0 } id) return false;
        var seconds = Duration(Text(node, "lengthText"));
        var title = Text(node, "title");
        if (seconds <= 0 || string.IsNullOrWhiteSpace(title)) return false;
        item = new VideoPlaylistItem(id, title, Text(node, "shortBylineText") ?? Text(node, "longBylineText") ?? "YouTube", seconds, Thumbnail(node) ?? $"https://i.ytimg.com/vi/{id}/hqdefault.jpg");
        return true;
    }

    private static bool TryLockupItem(JsonElement node, out VideoPlaylistItem? item)
    {
        item = null;
        if (!TryWatchEndpoint(node, out var videoId, out _) || string.IsNullOrWhiteSpace(videoId))
            return false;

        var metadata = LockupMetadata(node);
        var title = metadata.ValueKind == JsonValueKind.Object ? Text(metadata, "title") : null;
        var seconds = LockupDuration(node);
        if (string.IsNullOrWhiteSpace(title) || seconds <= 0) return false;

        item = new VideoPlaylistItem(
            videoId,
            title,
            LockupChannel(metadata) ?? "YouTube",
            seconds,
            LockupThumbnail(node) ?? $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg");
        return true;
    }

    private static JsonElement LockupMetadata(JsonElement node) =>
        node.TryGetProperty("metadata", out var metadata)
        && metadata.TryGetProperty("lockupMetadataViewModel", out var lockupMetadata)
            ? lockupMetadata
            : default;

    private static string? LockupChannel(JsonElement metadata)
    {
        if (metadata.ValueKind != JsonValueKind.Object
            || !metadata.TryGetProperty("metadata", out var nested))
            return null;

        return FirstText(nested);
    }

    private static string? FirstText(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("text", out var text) && Text(text, null) is { Length: > 0 } value)
                return value;

            foreach (var property in node.EnumerateObject())
            {
                var found = FirstText(property.Value);
                if (found is not null) return found;
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
            {
                var found = FirstText(child);
                if (found is not null) return found;
            }
        }

        return null;
    }

    private static bool TryWatchEndpoint(JsonElement node, out string? videoId, out string? playlistId)
    {
        videoId = null;
        playlistId = null;
        if (!node.TryGetProperty("rendererContext", out var rendererContext)
            || !rendererContext.TryGetProperty("commandContext", out var commandContext)
            || !commandContext.TryGetProperty("onTap", out var onTap)
            || !onTap.TryGetProperty("innertubeCommand", out var command)
            || !command.TryGetProperty("watchEndpoint", out var endpoint))
            return false;

        videoId = endpoint.TryGetProperty("videoId", out var videoEl) ? videoEl.GetString() : null;
        playlistId = endpoint.TryGetProperty("playlistId", out var playlistEl) ? playlistEl.GetString() : null;
        return !string.IsNullOrWhiteSpace(videoId) || !string.IsNullOrWhiteSpace(playlistId);
    }

    private static string? LockupThumbnail(JsonElement node)
    {
        if (!node.TryGetProperty("contentImage", out var contentImage)
            || !contentImage.TryGetProperty("thumbnailViewModel", out var viewModel)
            || !viewModel.TryGetProperty("image", out var image)
            || !image.TryGetProperty("sources", out var sources)
            || sources.ValueKind != JsonValueKind.Array)
            return null;

        return sources.EnumerateArray().LastOrDefault()
            .TryGetProperty("url", out var url) ? url.GetString() : null;
    }

    private static int LockupDuration(JsonElement node)
    {
        if (!node.TryGetProperty("contentImage", out var contentImage)) return 0;
        return FindDuration(contentImage);
    }

    private static int FindDuration(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("thumbnailBadgeViewModel", out var badge)
                && badge.TryGetProperty("text", out var text))
                return Duration(Text(text, null));

            foreach (var property in node.EnumerateObject())
            {
                var duration = FindDuration(property.Value);
                if (duration > 0) return duration;
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in node.EnumerateArray())
            {
                var duration = FindDuration(child);
                if (duration > 0) return duration;
            }
        }

        return 0;
    }

    private static string? FindText(JsonElement root, string rendererName, string field)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty(rendererName, out var renderer)) return Text(renderer, field);
            foreach (var property in root.EnumerateObject()) { var value = FindText(property.Value, rendererName, field); if (value is not null) return value; }
        }
        else if (root.ValueKind == JsonValueKind.Array) foreach (var item in root.EnumerateArray()) { var value = FindText(item, rendererName, field); if (value is not null) return value; }
        return null;
    }

    private static string? Text(JsonElement node, string? field)
    {
        var text = string.IsNullOrWhiteSpace(field) ? node : node.TryGetProperty(field, out var nested) ? nested : default;
        if (text.ValueKind == JsonValueKind.String) return text.GetString();
        if (text.ValueKind != JsonValueKind.Object) return null;
        if (text.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String) return content.GetString();
        if (text.TryGetProperty("simpleText", out var simple)) return simple.GetString();
        if (text.TryGetProperty("runs", out var runs) && runs.ValueKind == JsonValueKind.Array) return string.Concat(runs.EnumerateArray().Select(run => run.TryGetProperty("text", out var part) ? part.GetString() : null));
        return null;
    }

    private static string? Thumbnail(JsonElement node)
    {
        if (!node.TryGetProperty("thumbnail", out var thumbnail) || !thumbnail.TryGetProperty("thumbnails", out var images) || images.ValueKind != JsonValueKind.Array) return null;
        return images.EnumerateArray().LastOrDefault().TryGetProperty("url", out var url) ? url.GetString() : null;
    }

    private static int Duration(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var seconds = 0;
        foreach (var part in text.Split(':')) { if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) return 0; seconds = seconds * 60 + value; }
        return seconds;
    }
}
