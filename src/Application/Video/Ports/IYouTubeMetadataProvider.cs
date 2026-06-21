using Application.Video.Models;

namespace Application.Video.Ports;

/// <summary>
/// Fetches video metadata and captions from YouTube (Data API in production; a
/// deterministic Local stand-in for dev/tests). Calls are quota-limited, so the
/// ingestion command runs from a Hangfire background job, not the request path
/// (PROJECT-SPEC B.3, docs/development-guide.md rule 17.3).
/// </summary>
public interface IYouTubeMetadataProvider
{
    Task<VideoMetadata> FetchAsync(string youTubeVideoId, CancellationToken cancellationToken = default);
}
