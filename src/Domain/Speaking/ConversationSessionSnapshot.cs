namespace Domain.Speaking;

/// <summary>Serializable representation of a short-lived speaking session for distributed stores.</summary>
public sealed record ConversationSessionSnapshot(
    Guid Id,
    Guid LearnerId,
    Assessment.CefrLevel Level,
    string? Topic,
    IReadOnlyList<string> FocusWords,
    Guid? VocabularyTopicId,
    string? LearnerName,
    string? ScenarioCode,
    TimeSpan SpokenTime,
    DateTimeOffset? LastActivityAt,
    IReadOnlyList<ConversationTurnSnapshot> Turns,
    SpeakingCurriculumContext? CurriculumContext = null);

public sealed record ConversationTurnSnapshot(
    ConversationRole Role,
    string Text,
    PronunciationResultSnapshot? Pronunciation);

public sealed record PronunciationResultSnapshot(
    double OverallScore,
    double AccuracyScore,
    double FluencyScore,
    double CompletenessScore,
    IReadOnlyList<WordPronunciationSnapshot> Words);

public sealed record WordPronunciationSnapshot(
    string Word,
    double AccuracyScore,
    PronunciationErrorType ErrorType,
    IReadOnlyList<PhonemePronunciationSnapshot> Phonemes,
    string? SpokenForm = null);

public sealed record PhonemePronunciationSnapshot(string Phoneme, double AccuracyScore);
