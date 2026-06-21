using Domain.Common;

namespace Domain.Vocabulary;

/// <summary>
/// Tracks how much a learner has practiced speaking about a single <see cref="VocabularyTopic"/>
/// (PROJECT-SPEC module 4 ↔ Faza 1). A topic counts as <i>learned</i> once the learner has spoken
/// about it for <see cref="RequiredSpeakingTime"/> (5 minutes) across one or more conversations;
/// less than that and it stays unlearned. The accumulated time is engaged-speaking time measured by
/// <see cref="Speaking.ConversationSession"/>, not wall-clock, so it reflects real practice.
/// </summary>
public sealed class TopicSpeakingProgress
{
    private readonly HashSet<Guid> _sessionIds = new();
    private readonly HashSet<DateOnly> _practiceDays = new();
    private readonly List<int> _sessionScores = new();
    /// <summary>Speaking practice required before a topic is considered learned (the 5-minute rule).</summary>
    public static readonly TimeSpan RequiredSpeakingTime = TimeSpan.FromMinutes(5);

    // Parameterless ctor for EF Core materialization.
    private TopicSpeakingProgress()
    {
    }

    private TopicSpeakingProgress(Guid id, Guid learnerId, Guid vocabularyTopicId, DateTimeOffset now)
    {
        Id = id;
        LearnerId = learnerId;
        VocabularyTopicId = vocabularyTopicId;
        SpokenSeconds = 0;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public Guid VocabularyTopicId { get; private set; }

    /// <summary>Cumulative engaged-speaking time for this topic, in seconds.</summary>
    public double SpokenSeconds { get; private set; }

    /// <summary>When the learner crossed the 5-minute goal; null while the topic is unlearned.</summary>
    public DateTimeOffset? LearnedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsLearned => LearnedAt is not null;

    public TimeSpan SpokenTime => TimeSpan.FromSeconds(SpokenSeconds);
    public int SessionCount => _sessionIds.Count;
    public int PracticeDayCount => _practiceDays.Count;
    public bool HasSufficientPracticeEvidence => IsLearned && SessionCount >= 3 && PracticeDayCount >= 2;
    public int ConservativeMasteryScore => _sessionScores.Count == 0 ? 0 : _sessionScores.Min();

    public void RecordSession(Guid sessionId, DateTimeOffset occurredAt, int score)
    {
        if (sessionId == Guid.Empty)
            throw new DomainException("Session id must not be empty.");
        if (score is < 0 or > 100)
            throw new DomainException("Speaking score must be between 0 and 100.");
        if (!_sessionIds.Add(sessionId))
            return;
        _practiceDays.Add(DateOnly.FromDateTime(occurredAt.UtcDateTime));
        _sessionScores.Add(score);
        UpdatedAt = occurredAt;
    }

    public static TopicSpeakingProgress Start(Guid learnerId, Guid vocabularyTopicId, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (vocabularyTopicId == Guid.Empty)
            throw new DomainException("Vocabulary topic id must not be empty.");

        return new TopicSpeakingProgress(Guid.NewGuid(), learnerId, vocabularyTopicId, now);
    }

    /// <summary>
    /// Adds engaged-speaking time and marks the topic learned once the cumulative total reaches the
    /// 5-minute goal. A non-positive delta is ignored. Returns true only on the transition to
    /// learned (so the caller can celebrate it once), false otherwise.
    /// </summary>
    public bool AddSpeaking(TimeSpan delta, DateTimeOffset now)
    {
        if (delta <= TimeSpan.Zero)
            return false;

        SpokenSeconds += delta.TotalSeconds;
        UpdatedAt = now;

        if (LearnedAt is null && SpokenSeconds >= RequiredSpeakingTime.TotalSeconds)
        {
            LearnedAt = now;
            return true;
        }

        return false;
    }
}
