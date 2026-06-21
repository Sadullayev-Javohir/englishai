using Domain.Assessment;

namespace Domain.Curriculum;

public sealed class TopicSpeakingBlueprint
{
    private TopicSpeakingBlueprint() { Objective = PrimaryGrammarFocus = PriorityWordsJson = QuestionsJson = RubricJson = null!; }

    private TopicSpeakingBlueprint(Guid topicId, CefrLevel level, string objective, string primaryGrammarFocus,
        string? reviewGrammarFocus, string priorityWordsJson, string questionsJson, string rubricJson, int version, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); TopicId = topicId; Level = level; Objective = objective; PrimaryGrammarFocus = primaryGrammarFocus;
        ReviewGrammarFocus = reviewGrammarFocus; PriorityWordsJson = priorityWordsJson; QuestionsJson = questionsJson;
        RubricJson = rubricJson; CurriculumVersion = version; UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid TopicId { get; private set; }
    public CefrLevel Level { get; private set; }
    public string Objective { get; private set; }
    public string PrimaryGrammarFocus { get; private set; }
    public string? ReviewGrammarFocus { get; private set; }
    public string PriorityWordsJson { get; private set; }
    public string QuestionsJson { get; private set; }
    public string RubricJson { get; private set; }
    public int CurriculumVersion { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static TopicSpeakingBlueprint Create(Guid topicId, CefrLevel level, string objective,
        string primaryGrammarFocus, string? reviewGrammarFocus, string priorityWordsJson,
        string questionsJson, string rubricJson, int version, DateTimeOffset now) =>
        new(topicId, level, objective.Trim(), primaryGrammarFocus.Trim(), reviewGrammarFocus?.Trim(),
            priorityWordsJson, questionsJson, rubricJson, version, now);
}
