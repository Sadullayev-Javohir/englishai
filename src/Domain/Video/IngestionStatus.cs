namespace Domain.Video;

/// <summary>
/// Lifecycle of a curated video lesson (PROJECT-SPEC B.3). A video is first ingested
/// with its YouTube metadata (<see cref="Pending"/>), then auto-leveled to a CEFR band
/// by the LLM (<see cref="Leveled"/>) before it is shown in the adaptive catalog.
/// </summary>
public enum IngestionStatus
{
    /// <summary>Metadata fetched from YouTube; CEFR level not yet assigned.</summary>
    Pending = 0,

    /// <summary>CEFR level assigned; the lesson is ready to surface to learners.</summary>
    Leveled = 1
}
