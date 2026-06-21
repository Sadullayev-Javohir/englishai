using Application.Video.Models;

namespace Application.Video.Ports;

/// <summary>
/// Searches YouTube for English-learning videos to fill the adaptive video feed
/// (PROJECT-SPEC B.3, Bosqich 2). Implemented by the YouTube Data API adapter when a key
/// is configured, and by a keyless local results-scrape adapter otherwise - both swappable
/// behind this port (docs/development-guide.md rule 10). Results are English-only and caption-friendly;
/// pagination is driven by an opaque continuation token.
/// </summary>
public interface IVideoFeedSource
{
    /// <summary>
    /// Returns one page of results. On the first page <paramref name="continuation"/> is
    /// <c>null</c> and <paramref name="searchTerm"/> drives the search; on later pages the
    /// continuation token (from the previous <see cref="VideoFeedPage.NextContinuation"/>)
    /// is supplied and the search term is ignored.
    /// </summary>
    Task<VideoFeedPage> SearchAsync(
        string searchTerm, string? continuation, int pageSize, CancellationToken cancellationToken = default);
}
