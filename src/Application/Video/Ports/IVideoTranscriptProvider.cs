using Application.Video.Models;

namespace Application.Video.Ports;

/// <summary>
/// Fetches a video's real, time-synced transcript from YouTube's public caption surface
/// (PROJECT-SPEC B.3, Bosqich 3) - used to fill a lesson created with an empty transcript.
/// Implementations must never throw or fabricate: they report the outcome via
/// <see cref="TranscriptFetchResult"/> so the fill flow can tell a genuinely caption-less video
/// (terminal) apart from a transient provider failure (retry later), keeping the player's honest
/// state correct (docs/development-guide.md rules 8, 11).
/// </summary>
public interface IVideoTranscriptProvider
{
    /// <summary>
    /// The fetch outcome plus, on success, the timed English transcript lines (ordered by start
    /// time). Returns <see cref="TranscriptFetchOutcome.NoCaptions"/> only when the provider ran
    /// and confirmed the video has no caption track; any failure to run yields
    /// <see cref="TranscriptFetchOutcome.ProviderUnavailable"/> so the fill is retried, never made
    /// terminal.
    /// </summary>
    Task<TranscriptFetchResult> FetchAsync(string youTubeVideoId, CancellationToken cancellationToken = default);
}
