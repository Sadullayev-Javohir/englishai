using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Video.Models;

namespace Infrastructure.Video;

/// <summary>
/// Pure parsing helpers for YouTube's keyless search surface: the <c>ytInitialData</c> blob
/// embedded in the results HTML and the JSON returned by the <c>youtubei/v1/search</c>
/// continuation endpoint. Kept free of I/O so it is unit-testable against a captured fixture.
/// </summary>
internal static partial class YouTubeSearchParser
{
    /// <summary>Skip clips shorter than this - they are Shorts, not lessons.</summary>
    private const int MinDurationSeconds = 30;

    [GeneratedRegex("\"INNERTUBE_API_KEY\":\"([^\"]+)\"")]
    private static partial Regex InnertubeApiKeyRegex();

    [GeneratedRegex("\"INNERTUBE_CONTEXT_CLIENT_VERSION\":\"([^\"]+)\"")]
    private static partial Regex ClientVersionRegex();

    /// <summary>The youtubei API key + web client version needed to call the continuation endpoint.</summary>
    public static (string ApiKey, string ClientVersion)? ExtractInnertube(string html)
    {
        var key = InnertubeApiKeyRegex().Match(html);
        var version = ClientVersionRegex().Match(html);
        if (!key.Success || !version.Success)
            return null;

        return (key.Groups[1].Value, version.Groups[1].Value);
    }

    /// <summary>
    /// Extracts the <c>ytInitialData</c> JSON object from a results page. Returns <c>null</c>
    /// if the marker is absent (e.g. a consent/interstitial page).
    /// </summary>
    public static JsonDocument? ExtractInitialData(string html)
    {
        const string marker = "var ytInitialData = ";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return null;

        var braceStart = html.IndexOf('{', start);
        if (braceStart < 0)
            return null;

        var json = ExtractBalancedObject(html, braceStart);
        if (json is null)
            return null;

        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Collects the playable (non-Shorts) video results from any YouTube JSON tree.</summary>
    public static IReadOnlyList<VideoFeedResult> CollectVideos(JsonElement root)
    {
        var results = new List<VideoFeedResult>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        CollectVideos(root, results, seen);
        return results;
    }

    /// <summary>Finds the next-page continuation token in any YouTube JSON tree, or <c>null</c>.</summary>
    public static string? FindContinuationToken(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("continuationItemRenderer", out var cir)
                && TryReadContinuationToken(cir, out var token))
                return token;

            foreach (var prop in root.EnumerateObject())
            {
                var found = FindContinuationToken(prop.Value);
                if (found is not null)
                    return found;
            }
        }
        else if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                var found = FindContinuationToken(item);
                if (found is not null)
                    return found;
            }
        }

        return null;
    }

    private static void CollectVideos(JsonElement element, List<VideoFeedResult> results, HashSet<string> seen)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("videoRenderer", out var renderer)
                && TryParseRenderer(renderer, out var result)
                && seen.Add(result!.YouTubeVideoId))
            {
                results.Add(result);
            }

            foreach (var prop in element.EnumerateObject())
                CollectVideos(prop.Value, results, seen);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectVideos(item, results, seen);
        }
    }

    private static bool TryParseRenderer(JsonElement renderer, out VideoFeedResult? result)
    {
        result = null;

        if (!renderer.TryGetProperty("videoId", out var idEl) || idEl.GetString() is not { Length: > 0 } id)
            return false;

        // No lengthText ⇒ live stream or Shorts; skip - only fixed-length lessons belong in the feed.
        if (!renderer.TryGetProperty("lengthText", out var lengthEl))
            return false;

        var lengthText = ReadText(lengthEl);
        var durationSeconds = ParseDuration(lengthText);
        if (durationSeconds < MinDurationSeconds)
            return false;

        var title = renderer.TryGetProperty("title", out var titleEl) ? ReadText(titleEl) : null;
        if (string.IsNullOrWhiteSpace(title))
            return false;

        var channel =
            (renderer.TryGetProperty("ownerText", out var ownerEl) ? ReadText(ownerEl) : null)
            ?? (renderer.TryGetProperty("longBylineText", out var bylineEl) ? ReadText(bylineEl) : null)
            ?? "YouTube";

        result = new VideoFeedResult(
            id, title.Trim(), channel.Trim(), durationSeconds, HasClosedCaptionBadge(renderer),
            ReadChannelAvatar(renderer));
        return true;
    }

    private static string? ReadChannelAvatar(JsonElement renderer)
    {
        if (renderer.TryGetProperty("channelThumbnailSupportedRenderers", out var supported)
            && supported.TryGetProperty("channelThumbnailWithLinkRenderer", out var channel)
            && channel.TryGetProperty("thumbnail", out var thumbnail))
            return YouTubeChannelAvatars.FromThumbnails(thumbnail);

        return renderer.TryGetProperty("channelThumbnail", out var direct)
            ? YouTubeChannelAvatars.FromThumbnails(direct)
            : null;
    }

    private static bool HasClosedCaptionBadge(JsonElement renderer)
    {
        if (!renderer.TryGetProperty("badges", out var badges) || badges.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var badge in badges.EnumerateArray())
        {
            if (!badge.TryGetProperty("metadataBadgeRenderer", out var metadata))
                continue;

            if (metadata.TryGetProperty("label", out var label)
                && IsClosedCaptionLabel(label.GetString()))
                return true;

            if (metadata.TryGetProperty("accessibilityData", out var accessibility)
                && accessibility.TryGetProperty("label", out var accessibilityLabel)
                && IsClosedCaptionLabel(accessibilityLabel.GetString()))
                return true;
        }

        return false;
    }

    private static bool IsClosedCaptionLabel(string? value) =>
        string.Equals(value, "CC", StringComparison.OrdinalIgnoreCase)
        || value?.Contains("closed captions", StringComparison.OrdinalIgnoreCase) == true
        || value?.Contains("subtitles", StringComparison.OrdinalIgnoreCase) == true;

    private static bool TryReadContinuationToken(JsonElement continuationItemRenderer, out string? token)
    {
        token = null;
        if (continuationItemRenderer.TryGetProperty("continuationEndpoint", out var endpoint)
            && endpoint.TryGetProperty("continuationCommand", out var command)
            && command.TryGetProperty("token", out var tokenEl)
            && tokenEl.GetString() is { Length: > 0 } value)
        {
            token = value;
            return true;
        }

        return false;
    }

    /// <summary>Reads a YouTube text node, which is either <c>{simpleText}</c> or <c>{runs:[…]}</c>.</summary>
    private static string? ReadText(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return null;

        if (node.TryGetProperty("simpleText", out var simple))
            return simple.GetString();

        if (node.TryGetProperty("runs", out var runs) && runs.ValueKind == JsonValueKind.Array)
        {
            var text = string.Concat(runs.EnumerateArray()
                .Select(r => r.TryGetProperty("text", out var t) ? t.GetString() : null));
            return string.IsNullOrEmpty(text) ? null : text;
        }

        return null;
    }

    /// <summary>Parses a YouTube duration label ("m:ss" or "h:mm:ss") into seconds; 0 if unparseable.</summary>
    private static int ParseDuration(string? lengthText)
    {
        if (string.IsNullOrWhiteSpace(lengthText))
            return 0;

        var parts = lengthText.Split(':');
        var seconds = 0;
        foreach (var part in parts)
        {
            if (!int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return 0;
            seconds = seconds * 60 + value;
        }

        return seconds;
    }

    /// <summary>Returns the JSON object beginning at <paramref name="start"/> ('{'), brace-balanced and string-aware.</summary>
    private static string? ExtractBalancedObject(string text, int start)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inString = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    break;
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                        return text.Substring(start, i - start + 1);
                    break;
            }
        }

        return null;
    }
}
