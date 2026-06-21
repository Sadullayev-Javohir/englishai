namespace Domain.Speaking;

public sealed record SpeakingCurriculumContext(
    Guid? TopicId,
    string TopicTitle,
    string? Objective,
    string? PrimaryGrammarFocus,
    string? ReviewGrammarFocus,
    IReadOnlyList<string> PriorityWords,
    IReadOnlyList<string> Questions);
