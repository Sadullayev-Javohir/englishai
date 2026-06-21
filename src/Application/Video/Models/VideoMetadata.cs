using Domain.Video;

namespace Application.Video.Models;

/// <summary>
/// One timed line returned by a metadata/caption provider for a video. <see cref="Words"/> carries
/// the line's per-word timing when the source provides it (YouTube json3 captions), so the player
/// can highlight the exact spoken word; it is <c>null</c> when the source gives line-level timing only.
/// </summary>
public sealed record TranscriptLine(
    double StartSeconds,
    double EndSeconds,
    string EnglishText,
    string? UzbekTranslation = null,
    IReadOnlyList<TranscriptWord>? Words = null);

/// <summary>
/// Raw metadata fetched from YouTube (Data API) for a video, before it is leveled and
/// stored. The transcript comes from captions or Azure STT (PROJECT-SPEC B.3); the Uzbek
/// translation, if any, is curated content added separately (docs/development-guide.md rule 11).
/// </summary>
public sealed record VideoMetadata(
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    IReadOnlyList<TranscriptLine> Transcript);
