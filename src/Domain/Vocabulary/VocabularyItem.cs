using Domain.Common;

namespace Domain.Vocabulary;

/// <summary>
/// Aggregate root for a word/phrase a learner is studying (PROJECT-SPEC B.1). It owns
/// the word's spaced-repetition <see cref="ReviewSchedule"/> and decides which active
/// mini-test to present next. The Uzbek <see cref="Translation"/> is learner/content
/// data (not free-generated UI text), so it is stored as-is per docs/development-guide.md rule 11.
/// </summary>
public sealed class VocabularyItem
{
    // Parameterless ctor for EF Core materialization.
    private VocabularyItem()
    {
        Word = null!;
        Translation = null!;
        Schedule = null!;
    }

    private VocabularyItem(
        Guid learnerId,
        string word,
        string translation,
        string? exampleSentence,
        VocabularySource source,
        Guid? sourceTopicId,
        PartOfSpeech partOfSpeech,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Word = word;
        Translation = translation;
        ExampleSentence = exampleSentence;
        Source = source;
        SourceTopicId = sourceTopicId;
        PartOfSpeech = partOfSpeech;
        CreatedAt = now;
        Schedule = ReviewSchedule.StartNew(now);
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }

    /// <summary>The English word or phrase being learned.</summary>
    public string Word { get; private set; }

    /// <summary>The vetted Uzbek translation/meaning shown to the learner.</summary>
    public string Translation { get; private set; }

    /// <summary>Optional context sentence from where the word was encountered.</summary>
    public string? ExampleSentence { get; private set; }

    public VocabularySource Source { get; private set; }

    /// <summary>
    /// The vocabulary topic this word was learned from, when it came from a topic's word list
    /// (PROJECT-SPEC module 4). Lets the SRS review show the word's topic image (Uzbek↔English
    /// picture cards). Null for words added from other sources (manual, speaking, video, ...).
    /// </summary>
    public Guid? SourceTopicId { get; private set; }

    /// <summary>
    /// The word's grammatical category, carried over from the <see cref="TopicWord"/> it was learned
    /// from (or <see cref="Domain.Vocabulary.PartOfSpeech.Other"/> for manually added words). Lets the
    /// SRS review and "Mening so'zlarim" list show a word-class chip (noun/verb/adjective/…), so the
    /// learner knows whether they are revising an adjective or a noun.
    /// </summary>
    public PartOfSpeech PartOfSpeech { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ReviewSchedule Schedule { get; private set; }

    /// <summary>
    /// Records a newly learned word and schedules its first review (day 3).
    /// </summary>
    public static VocabularyItem Learn(
        Guid learnerId,
        string word,
        string translation,
        DateTimeOffset now,
        string? exampleSentence = null,
        VocabularySource source = VocabularySource.Manual,
        Guid? sourceTopicId = null,
        PartOfSpeech partOfSpeech = PartOfSpeech.Other)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (string.IsNullOrWhiteSpace(word))
            throw new DomainException("Word must not be empty.");
        if (string.IsNullOrWhiteSpace(translation))
            throw new DomainException("Translation must not be empty.");

        return new VocabularyItem(
            learnerId, word.Trim(), translation.Trim(), exampleSentence?.Trim(), source, sourceTopicId,
            partOfSpeech, now);
    }

    /// <summary>True when this word is due for review as of <paramref name="now"/>.</summary>
    public bool IsDue(DateTimeOffset now) => Schedule.IsDue(now);

    /// <summary>Records the outcome of a review, updating the schedule (PROJECT-SPEC B.1).</summary>
    public void RecordReview(bool passed, DateTimeOffset now) => Schedule.RecordResult(passed, now);

    /// <summary>
    /// Forces this word to be due for review right now (testing/operator aid). Its learning stage
    /// and fail count are preserved; a mastered word is revived to the first checkpoint. See
    /// <see cref="ReviewSchedule.MakeDueNow"/>.
    /// </summary>
    public void MakeDueForReview(DateTimeOffset now) => Schedule.MakeDueNow(now);

    /// <summary>
    /// The mini-test to present at the current checkpoint. The type changes with the
    /// stage so each review exercises the word in a new way (PROJECT-SPEC B.1): cloze
    /// choice → written usage → spoken usage. Listening recognition is reserved for
    /// words first met in a video/audio context.
    /// </summary>
    public MiniTestType NextMiniTestType()
    {
        if (Source == VocabularySource.Video)
            return MiniTestType.ListeningRecognition;

        return Schedule.Stage switch
        {
            ReviewStage.Day3 => MiniTestType.ClozeChoice,
            ReviewStage.Day7 => MiniTestType.WrittenUsage,
            ReviewStage.Day21 => MiniTestType.SpokenUsage,
            _ => MiniTestType.ClozeChoice
        };
    }
}
