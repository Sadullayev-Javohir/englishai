using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// Full pronunciation assessment of one learner utterance: the overall score plus
/// the Azure-style sub-scores and the per-word breakdown.
/// </summary>
public sealed class PronunciationResult
{
    private readonly List<WordPronunciation> _words;

    public PronunciationResult(
        double overallScore,
        double accuracyScore,
        double fluencyScore,
        double completenessScore,
        IReadOnlyList<WordPronunciation> words,
        bool isAuthentic = true)
    {
        if (overallScore is < 0 or > 100)
            throw new DomainException("Overall score must be between 0 and 100.");

        OverallScore = overallScore;
        AccuracyScore = accuracyScore;
        FluencyScore = fluencyScore;
        CompletenessScore = completenessScore;
        IsAuthentic = isAuthentic;
        _words = new List<WordPronunciation>(words);
    }

    public double OverallScore { get; }
    public double AccuracyScore { get; }
    public double FluencyScore { get; }
    public double CompletenessScore { get; }
    public bool IsAuthentic { get; }
    public IReadOnlyList<WordPronunciation> Words => _words;

    public PronunciationBand Band =>
        OverallScore >= SpeakingScoreThresholds.GoodOverall
            ? PronunciationBand.Good
            : PronunciationBand.NeedsImprovement;

    /// <summary>Words the learner should practice, weakest first.</summary>
    public IReadOnlyList<WordPronunciation> WordsNeedingPractice =>
        _words.Where(w => w.NeedsPractice).OrderBy(w => w.AccuracyScore).ToList();
}
