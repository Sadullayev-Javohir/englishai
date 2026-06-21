namespace Application.Video.Dtos;

public sealed record VideoPlaylistItemDto(
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    string? ThumbnailUrl);

public sealed record VideoPlaylistDto(
    string Id,
    string Title,
    string Channel,
    string? ThumbnailUrl,
    IReadOnlyList<VideoPlaylistItemDto> Items,
    int TotalDurationSeconds,
    bool IsSingleVideoCollection);

public sealed record VideoPlaylistSearchDto(IReadOnlyList<VideoPlaylistDto> Items);
