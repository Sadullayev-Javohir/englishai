using Domain.Common;

namespace Domain.Video;

/// <summary>
/// One spoken word of a transcript segment with its own start/end offset in the video, so the
/// player can highlight exactly the word being said (karaoke-style, PROJECT-SPEC B.3, Bosqich 3).
/// The timing is real - extracted from YouTube's per-word caption offsets, not estimated. Persisted
/// as part of the owning segment's <c>Words</c> JSON column.
/// </summary>
public sealed record TranscriptWord
{
    // Positional-style ctor used both by EF JSON materialization and by Create after validation.
    public TranscriptWord(string text, double startSeconds, double endSeconds)
    {
        Text = text;
        StartSeconds = startSeconds;
        EndSeconds = endSeconds;
    }

    /// <summary>The word's surface text, exactly as spoken (e.g. "I'm", "stanford.edu").</summary>
    public string Text { get; init; }

    /// <summary>Offset, in seconds, where this word starts in the video.</summary>
    public double StartSeconds { get; init; }

    /// <summary>Offset, in seconds, where this word ends (always &gt; <see cref="StartSeconds"/>).</summary>
    public double EndSeconds { get; init; }

    /// <summary>Builds a validated word, trimming the text and guaranteeing end &gt; start.</summary>
    public static TranscriptWord Create(string text, double startSeconds, double endSeconds)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Transcript word text must not be empty.");
        if (startSeconds < 0)
            throw new DomainException("Transcript word start must not be negative.");
        if (endSeconds <= startSeconds)
            throw new DomainException("Transcript word end must come after its start.");

        return new TranscriptWord(text.Trim(), startSeconds, endSeconds);
    }
}
