using System.Text.Json;

namespace Infrastructure.Video;

/// <summary>Reads channel artwork only; a video's thumbnail is never a channel avatar.</summary>
internal static class YouTubeChannelAvatars
{
    public static string? FromThumbnails(JsonElement thumbnail)
    {
        if (!thumbnail.TryGetProperty("thumbnails", out var images) || images.ValueKind != JsonValueKind.Array)
            return null;

        return images.EnumerateArray()
            .OrderByDescending(image => image.TryGetProperty("width", out var width) && width.TryGetInt32(out var value) ? value : 0)
            .Select(image => image.TryGetProperty("url", out var url) ? Normalize(url.GetString()) : null)
            .FirstOrDefault(url => url is not null);
    }

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith("//", StringComparison.Ordinal)) value = $"https:{value}";
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.AbsoluteUri
            : null;
    }
}
