using Application.Video.Models;

namespace Application.Video.Ports;

/// <summary>
/// Finds publicly embeddable long-form YouTube collections. Implementations may use the
/// YouTube Data API or public YouTube result pages, but never manufacture a playlist.
/// </summary>
public interface IVideoPlaylistDiscoverySource
{
    Task<IReadOnlyList<VideoPlaylist>> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default);
    Task<VideoPlaylist?> GetAsync(string playlistId, CancellationToken cancellationToken = default);
}
