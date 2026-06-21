using Domain.Common;

namespace Domain.Video;

/// <summary>
/// One timed line of a video's interactive transcript (Video Player screen). The English
/// text comes from the captions/STT; the optional <see cref="UzbekTranslation"/> is
/// vetted curated content (not free-generated UI text), stored as-is per docs/development-guide.md
/// rule 11 - the same treatment as a vocabulary translation.
/// </summary>
public sealed class TranscriptSegment
{
    private readonly List<TranscriptWord> _words = new();

    // Parameterless ctor for EF Core materialization.
    private TranscriptSegment()
    {
        EnglishText = null!;
    }

    private TranscriptSegment(
        double startSeconds, double endSeconds, string englishText, string? uzbekTranslation,
        IEnumerable<TranscriptWord>? words)
    {
        Id = Guid.NewGuid();
        StartSeconds = startSeconds;
        EndSeconds = endSeconds;
        EnglishText = englishText;
        UzbekTranslation = uzbekTranslation;

        if (words is not null)
            foreach (var word in words.OrderBy(w => w.StartSeconds))
                if (word is not null && !string.IsNullOrWhiteSpace(word.Text))
                    _words.Add(word);
    }

    public Guid Id { get; private set; }

    /// <summary>Offset, in seconds, where this line starts in the video.</summary>
    public double StartSeconds { get; private set; }

    /// <summary>Offset, in seconds, where this line ends in the video.</summary>
    public double EndSeconds { get; private set; }

    /// <summary>The spoken English for this line.</summary>
    public string EnglishText { get; private set; }

    /// <summary>Optional vetted Uzbek translation shown under the line.</summary>
    public string? UzbekTranslation { get; private set; }

    /// <summary>
    /// The segment's words with their own timing, used for the player's karaoke word highlight.
    /// Empty for transcripts whose source gave no per-word timing (e.g. manual captions); the
    /// player then falls back to estimating the spoken word from the line span.
    /// </summary>
    public IReadOnlyList<TranscriptWord> Words => _words;

    public static TranscriptSegment Create(
        double startSeconds, double endSeconds, string englishText, string? uzbekTranslation = null,
        IEnumerable<TranscriptWord>? words = null)
    {
        if (startSeconds < 0)
            throw new DomainException("Transcript start must not be negative.");
        if (endSeconds <= startSeconds)
            throw new DomainException("Transcript end must come after its start.");
        if (string.IsNullOrWhiteSpace(englishText))
            throw new DomainException("Transcript line text must not be empty.");

        return new TranscriptSegment(
            startSeconds, endSeconds, englishText.Trim(),
            string.IsNullOrWhiteSpace(uzbekTranslation) ? null : uzbekTranslation.Trim(),
            words);
    }
}
