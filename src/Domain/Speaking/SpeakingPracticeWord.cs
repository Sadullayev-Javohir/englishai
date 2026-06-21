using Domain.Common;

namespace Domain.Speaking;

public sealed class SpeakingPracticeWord
{
    private SpeakingPracticeWord() { }

    private SpeakingPracticeWord(
        Guid id,
        Guid learnerId,
        string word,
        string normalizedWord,
        double lastAccuracyScore,
        PronunciationErrorType lastErrorType,
        DateTimeOffset now)
    {
        Id = id;
        LearnerId = learnerId;
        Word = word;
        NormalizedWord = normalizedWord;
        LastAccuracyScore = lastAccuracyScore;
        LastErrorType = lastErrorType;
        ErrorCount = 1;
        FirstFailedAt = now;
        LastFailedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public string Word { get; private set; } = string.Empty;
    public string NormalizedWord { get; private set; } = string.Empty;
    public double LastAccuracyScore { get; private set; }
    public PronunciationErrorType LastErrorType { get; private set; }
    public int ErrorCount { get; private set; }
    public double? BestPracticeScore { get; private set; }
    public int SuccessfulAttemptCount { get; private set; }
    public DateTimeOffset FirstFailedAt { get; private set; }
    public DateTimeOffset LastFailedAt { get; private set; }
    public DateTimeOffset? MasteredAt { get; private set; }

    public bool IsActive => MasteredAt is null;

    public static SpeakingPracticeWord Create(
        Guid learnerId,
        string word,
        double accuracyScore,
        PronunciationErrorType errorType,
        DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        var normalizedWord = Normalize(word);
        return new SpeakingPracticeWord(
            Guid.NewGuid(), learnerId, word.Trim(), normalizedWord, accuracyScore, errorType, now);
    }

    public void RecordFailure(
        string word,
        double accuracyScore,
        PronunciationErrorType errorType,
        DateTimeOffset now)
    {
        if (Normalize(word) != NormalizedWord)
            throw new DomainException("Practice word does not match.");

        Word = word.Trim();
        LastAccuracyScore = accuracyScore;
        LastErrorType = errorType;
        LastFailedAt = now;
        ErrorCount++;
        // The word regressed, so any progress toward the 3-success mastery streak is lost.
        SuccessfulAttemptCount = 0;
        MasteredAt = null;
    }

    public void RecordPractice(double score, DateTimeOffset now)
    {
        BestPracticeScore = Math.Max(BestPracticeScore ?? 0, score);
        if (score < SpeakingPracticeThresholds.MasteryScore)
            return;

        SuccessfulAttemptCount++;
        if (SuccessfulAttemptCount >= SpeakingPracticeThresholds.RequiredSuccesses)
            MasteredAt = now;
    }

    public static string Normalize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new DomainException("Word must not be empty.");

        var normalized = new string(word.Trim().ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character == '\'')
            .ToArray());

        if (string.IsNullOrWhiteSpace(normalized))
            throw new DomainException("Word must contain letters or digits.");

        return normalized;
    }
}
