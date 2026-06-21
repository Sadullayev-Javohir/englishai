namespace Application.Video.Models;

/// <summary>One publicly embeddable item inside a discovered long-form YouTube collection.</summary>
public sealed record VideoPlaylistItem(
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    string? ThumbnailUrl = null);

/// <summary>
/// A long-form playlist (or a one-item full-length collection) discovered fresh from YouTube.
/// The source never treats a short clip as a full film: every returned item has a real duration.
/// </summary>
public sealed record VideoPlaylist(
    string Id,
    string Title,
    string Channel,
    string? ThumbnailUrl,
    IReadOnlyList<VideoPlaylistItem> Items,
    bool IsSingleVideoCollection = false)
{
    public int TotalDurationSeconds => Items.Sum(item => item.DurationSeconds);
}
