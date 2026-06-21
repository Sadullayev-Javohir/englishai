using Application.Video.Dtos;
using Application.Video.Models;
using Application.Video.Ports;
using MediatR;
using System.Threading;

namespace Application.Video.FindFullVideoPlaylist;

public sealed class FindFullVideoPlaylistQueryHandler :
    IRequestHandler<FindFullVideoPlaylistQuery, VideoPlaylistSearchDto>,
    IRequestHandler<GetFullVideoPlaylistQuery, VideoPlaylistDto?>,
    IRequestHandler<GetFeaturedVideoPlaylistQuery, VideoPlaylistDto?>
{
    private static readonly string[] FeaturedQueries =
    {
        "Puss in Boots full episodes English playlist",
        "Inside Out full movie English playlist",
        "Shrek full episodes English playlist",
        "Kung Fu Panda full episodes English playlist",
        "How to Train Your Dragon full episodes English playlist",
        "The Incredibles full episodes English playlist",
        "Interstellar full movie English playlist",
        "Captain America full movie English playlist",
        "Avengers full movie English playlist",
        "The Martian full movie English playlist",
        "Harry Potter full movie English playlist",
        "Pirates of the Caribbean full movie English playlist",
        "The Hunger Games full movie English playlist",
        "Jurassic Park full movie English playlist",
    };
    private static int _featuredQueryStart = -1;

    private readonly IVideoPlaylistDiscoverySource _source;

    public FindFullVideoPlaylistQueryHandler(IVideoPlaylistDiscoverySource source) => _source = source;

    public async Task<VideoPlaylistSearchDto> Handle(FindFullVideoPlaylistQuery request, CancellationToken cancellationToken) =>
        new((await _source.SearchAsync(request.Query.Trim(), 8, cancellationToken)).Select(ToDto).ToList());

    public async Task<VideoPlaylistDto?> Handle(GetFullVideoPlaylistQuery request, CancellationToken cancellationToken)
    {
        var playlist = await _source.GetAsync(request.PlaylistId, cancellationToken);
        return playlist is null ? null : ToDto(playlist);
    }

    public async Task<VideoPlaylistDto?> Handle(GetFeaturedVideoPlaylistQuery request, CancellationToken cancellationToken)
    {
        // Advance on every fresh page request, rather than once per day. This stops the feature
        // card from becoming a permanently repeated Avengers/Puss-in-Boots result in a user's session.
        var start = (int)((uint)Interlocked.Increment(ref _featuredQueryStart) % (uint)FeaturedQueries.Length);
        for (var offset = 0; offset < FeaturedQueries.Length; offset++)
        {
            var query = FeaturedQueries[(start + offset) % FeaturedQueries.Length];
            var playlist = (await _source.SearchAsync(query, 1, cancellationToken)).FirstOrDefault();
            if (playlist is not null) return ToDto(playlist);
        }
        return null;
    }

    private static VideoPlaylistDto ToDto(VideoPlaylist playlist) => new(
        playlist.Id,
        playlist.Title,
        playlist.Channel,
        playlist.ThumbnailUrl,
        playlist.Items.Select(item => new VideoPlaylistItemDto(item.YouTubeVideoId, item.Title, item.Channel, item.DurationSeconds, item.ThumbnailUrl)).ToList(),
        playlist.TotalDurationSeconds,
        playlist.IsSingleVideoCollection);
}
