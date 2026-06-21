namespace Domain.Video;

/// <summary>
/// State of a video lesson's interactive transcript (PROJECT-SPEC B.3, Bosqich 3). A lesson is
/// created <see cref="Pending"/>; the background fill then either populates real lines from the
/// video's captions or Azure STT (<see cref="Available"/>), or - when neither yields anything -
/// settles on the terminal <see cref="Unavailable"/> state so the player stops polling and shows
/// an honest "no transcript" message instead of an endless "preparing…" spinner (docs/development-guide.md rule 8).
///
/// For a long video the fill streams the transcript in chunks: the first slice (~5% from the
/// start) is saved as <see cref="Partial"/> so the player shows real text fast, then the rest is
/// appended chunk by chunk; only the last append flips it to <see cref="Available"/>. The player
/// keeps polling while it is <see cref="Pending"/> or <see cref="Partial"/>.
/// </summary>
public enum TranscriptStatus
{
    /// <summary>Not yet attempted; the player polls while the background fill runs.</summary>
    Pending = 0,

    /// <summary>Real transcript lines are present and complete (from captions or speech-to-text).</summary>
    Available = 1,

    /// <summary>Fill attempted (captions + STT) and produced nothing - terminal, never fabricated.</summary>
    Unavailable = 2,

    /// <summary>
    /// Some real lines are present but the fill is still streaming the rest (long video chunked
    /// fill). Non-terminal: the player shows what it has and keeps polling for the remainder.
    /// </summary>
    Partial = 3
}
