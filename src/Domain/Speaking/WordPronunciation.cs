using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// Pronunciation outcome for one spoken word, including its per-phoneme breakdown.
/// </summary>
public sealed class WordPronunciation
{
    private readonly List<PhonemePronunciation> _phonemes;

    public WordPronunciation(
        string word,
        double accuracyScore,
        PronunciationErrorType errorType,
        IReadOnlyList<PhonemePronunciation>? phonemes = null,
        string? spokenForm = null)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new DomainException("Word must not be empty.");

        Word = word;
        AccuracyScore = accuracyScore;
        ErrorType = NormalizeErrorType(accuracyScore, errorType);
        SpokenForm = string.IsNullOrWhiteSpace(spokenForm) ? null : spokenForm.Trim();
        _phonemes = phonemes is null ? new List<PhonemePronunciation>() : new List<PhonemePronunciation>(phonemes);
    }

    public string Word { get; }
    public double AccuracyScore { get; }
    public PronunciationErrorType ErrorType { get; }
    public string? SpokenForm { get; }
    public IReadOnlyList<PhonemePronunciation> Phonemes => _phonemes;

    /// <summary>True when this word should be surfaced for practice.</summary>
    public bool NeedsPractice =>
        ErrorType != PronunciationErrorType.None ||
        AccuracyScore < SpeakingScoreThresholds.WordNeedsPractice;

    /// <summary>The lowest-scoring phoneme, useful for the pronunciation detail view.</summary>
    public PhonemePronunciation? WeakestPhoneme =>
        _phonemes.Count == 0 ? null : _phonemes.MinBy(p => p.AccuracyScore);

    private static PronunciationErrorType NormalizeErrorType(
        double accuracyScore,
        PronunciationErrorType errorType) =>
        errorType == PronunciationErrorType.Mispronunciation
        && accuracyScore >= SpeakingScoreThresholds.WordNeedsPractice
            ? PronunciationErrorType.None
            : errorType;
}
