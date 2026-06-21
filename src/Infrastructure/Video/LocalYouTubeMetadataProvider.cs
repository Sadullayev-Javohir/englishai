using Application.Video.Models;
using Application.Video.Ports;
using Infrastructure.Common;

namespace Infrastructure.Video;

/// <summary>
/// Deterministic local stand-in for the YouTube metadata provider. Produces stable,
/// plausible metadata and a short English transcript for any video id so the ingestion
/// pipeline is exercisable without a YouTube API key. Replace with
/// <see cref="YouTubeMetadataProvider"/> when a key is configured.
/// </summary>
public sealed class LocalYouTubeMetadataProvider : IYouTubeMetadataProvider
{
    public Task<VideoMetadata> FetchAsync(string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        // Derive a stable duration (3–9 minutes) from the id so results are deterministic.
        var seed = Math.Abs(DeterministicGuid.Create(youTubeVideoId).GetHashCode());
        var durationSeconds = 180 + seed % 360;

        var transcript = new List<TranscriptLine>
        {
            new(0, 6, "Welcome to today's lesson about everyday English."),
            new(6, 13, "We will look at how people talk about their daily lives and plans."),
            new(13, 20, "Listening carefully and practising every day is the key to progress."),
        };

        var metadata = new VideoMetadata(
            youTubeVideoId,
            "Learning English Lesson",
            "Open Educational Channel",
            durationSeconds,
            transcript);

        return Task.FromResult(metadata);
    }
}
