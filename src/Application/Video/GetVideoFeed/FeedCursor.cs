using System.Text;
using System.Text.Json;

namespace Application.Video.GetVideoFeed;

/// <summary>
/// The opaque page cursor for the video feed: the active search term plus the source's
/// continuation token, packed into a single URL-safe string. The client treats it as
/// opaque and only echoes it back; encoding both pieces keeps the request self-contained.
/// </summary>
internal sealed record FeedCursor(string SearchTerm, string? Continuation)
{
    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    /// <summary>Decodes a cursor, or returns <c>null</c> for a missing/malformed value.</summary>
    public static FeedCursor? TryDecode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var decoded = JsonSerializer.Deserialize<FeedCursor>(json);
            return string.IsNullOrWhiteSpace(decoded?.SearchTerm) ? null : decoded;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or DecoderFallbackException)
        {
            return null;
        }
    }
}
