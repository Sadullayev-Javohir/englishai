namespace Application.Video.Models;

/// <summary>
/// One discovered video returned by the feed source (search), before it is opened into a
/// stored lesson. Only embeddable metadata is carried - no file is stored (docs/development-guide.md rule 17.3).
/// </summary>
public sealed record VideoFeedResult(
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    bool HasClosedCaptions = false,
    string? ChannelAvatarUrl = null);

/// <summary>
/// A page of feed results plus an opaque <see cref="NextContinuation"/> token to fetch the
/// next page (<c>null</c> when the source has no more). The token is provider-internal: the
/// query layer treats it as opaque and only round-trips it back to the same source.
/// </summary>
public sealed record VideoFeedPage(
    IReadOnlyList<VideoFeedResult> Items,
    string? NextContinuation);
