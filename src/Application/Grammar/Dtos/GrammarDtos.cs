using Application.Grammar.Content;
using Application.Vocabulary;
using Application.Vocabulary.Dtos;
using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using Domain.Vocabulary;

namespace Application.Grammar.Dtos;

/// <summary>
/// A catalog row for the grammar list. Each row is one learning-spine topic - every skill teaches
/// the same 50 topics per level - so the id here is the topic id, and the grammar lesson (the
/// topic's grammar focus taught in its own context) is generated lazily when the topic is opened.
/// </summary>
public sealed record GrammarTopicSummaryDto(
    Guid TopicId,
    string Title,
    string TitleUz,
    string Category,
    CefrLevel Level,
    string GrammarFocusCode)
{
    public static GrammarTopicSummaryDto FromTopic(VocabularyTopic topic) =>
        new(topic.Id, topic.Title, topic.TitleUz, topic.Category, topic.Level, topic.GrammarFocusCode);
}

/// <summary>
/// A grammar exercise as shown to the learner - the correct answer is intentionally omitted
/// so grading happens server-side (revealed only in the result).
/// </summary>
public sealed record GrammarExerciseDto(
    Guid Id,
    GrammarExerciseType Type,
    string Prompt,
    IReadOnlyList<string> Options)
{
    public static GrammarExerciseDto FromDomain(GrammarExercise exercise) =>
        new(exercise.Id, exercise.Type, exercise.Prompt, exercise.Options);
}

/// <summary>Step 5 application task: a guided Speaking/Writing prompt.</summary>
public sealed record GrammarApplicationTaskDto(Guid Id, SkillType TargetSkill, string Prompt)
{
    public static GrammarApplicationTaskDto FromDomain(GrammarApplicationTask task) =>
        new(task.Id, task.TargetSkill, task.Prompt);
}

/// <summary>One example sentence in a generated/curated lesson: the exact English and its Uzbek meaning.</summary>
public sealed record GrammarExampleDto(string English, string Uzbek)
{
    public static GrammarExampleDto FromDomain(GrammarExample example) =>
        new(example.English, example.Uzbek);
}

/// <summary>One explanation block of a curated lesson: a short Uzbek heading and its Uzbek body.</summary>
public sealed record CuratedGrammarRuleDto(string HeadingUz, string BodyUz)
{
    public static CuratedGrammarRuleDto FromContent(CuratedGrammarRule rule) =>
        new(rule.HeadingUz, rule.BodyUz);
}

/// <summary>
/// The vetted, hand-authored Uzbek explanation of a topic's grammar focus (docs/development-guide.md rule 11). When
/// present, the page shows this as the primary Step-2 content instead of the on-demand translation of
/// the generated English rule, so the Uzbek is always accurate and the English is kept only where it
/// must be exact - the formulas and the example sentences.
/// </summary>
public sealed record CuratedGrammarLessonDto(
    string TitleUz,
    string SummaryUz,
    IReadOnlyList<string> Formulas,
    IReadOnlyList<CuratedGrammarRuleDto> Rules,
    IReadOnlyList<GrammarExampleDto> Examples,
    IReadOnlyList<string> CommonMistakesUz)
{
    public static CuratedGrammarLessonDto FromContent(CuratedGrammarLesson lesson) =>
        new(lesson.TitleUz, lesson.SummaryUz, lesson.Formulas,
            lesson.Rules.Select(CuratedGrammarRuleDto.FromContent).ToList(),
            lesson.Examples.Select(e => new GrammarExampleDto(e.English, e.Uzbek)).ToList(),
            lesson.CommonMistakesUz);

    /// <summary>The curated Uzbek lesson for a grammar focus code, or null if none is authored yet.</summary>
    public static CuratedGrammarLessonDto? ForFocus(string? grammarFocusCode)
    {
        var lesson = CuratedGrammarLessonCatalog.TryGet(grammarFocusCode);
        return lesson is null ? null : FromContent(lesson);
    }
}

/// <summary>
/// Full grammar-lesson detail keyed by its learning-spine topic (all five G.2 steps): the context
/// intro, the generated English rule explanation, the (answerless) exercises and the application
/// tasks. When <see cref="IsReady"/> is false the content is still being generated (the lesson is
/// pending), so the page shows an honest "preparing" state rather than fake text (rules 8, 11).
/// </summary>
public sealed record GrammarLessonDto(
    Guid TopicId,
    string Topic,
    ErrorCategory Category,
    CefrLevel Level,
    bool IsReady,
    string ContextIntro,
    string Explanation,
    IReadOnlyList<GrammarExampleDto> Examples,
    IReadOnlyList<string> CommonMistakesUz,
    IReadOnlyList<GrammarExerciseDto> Exercises,
    IReadOnlyList<GrammarApplicationTaskDto> ApplicationTasks,
    CuratedGrammarLessonDto? Curated,
    IReadOnlyList<TargetWordDto> TargetWords)
{
    public string? GrammarFocusCode { get; init; }

    public static GrammarLessonDto FromDomain(VocabularyTopic topic, GrammarLesson lesson) =>
        new(topic.Id, lesson.Topic, lesson.Category, lesson.Level, lesson.IsFilled,
            lesson.ContextIntro, lesson.Explanation ?? string.Empty,
            lesson.Examples.Select(GrammarExampleDto.FromDomain).ToList(),
            lesson.CommonMistakes.Select(m => m.Text).ToList(),
            lesson.Exercises.Select(GrammarExerciseDto.FromDomain).ToList(),
            lesson.ApplicationTasks.Select(GrammarApplicationTaskDto.FromDomain).ToList(),
            lesson.CuratedRules.Count > 0
                ? new CuratedGrammarLessonDto(
                    lesson.CuratedTitleUz ?? lesson.Topic,
                    lesson.CuratedSummaryUz ?? string.Empty,
                    lesson.CuratedFormulas,
                    lesson.CuratedRules.Select(item => new CuratedGrammarRuleDto(item.HeadingUz, item.BodyUz)).ToList(),
                    lesson.Examples.Select(GrammarExampleDto.FromDomain).ToList(),
                    lesson.CommonMistakes.Select(item => item.Text).ToList())
                : CuratedGrammarLessonDto.ForFocus(lesson.GrammarFocusCode),
            TopicTargetWords.DetailedOf(topic)) { GrammarFocusCode = lesson.GrammarFocusCode };

    /// <summary>A pending placeholder for a topic whose grammar lesson is not generated yet.</summary>
    public static GrammarLessonDto Pending(VocabularyTopic topic) =>
        new(topic.Id, topic.Title, GrammarFocus(topic), topic.Level, false,
            string.Empty, string.Empty,
            Array.Empty<GrammarExampleDto>(), Array.Empty<string>(),
            Array.Empty<GrammarExerciseDto>(), Array.Empty<GrammarApplicationTaskDto>(),
            CuratedGrammarLessonDto.ForFocus(topic.GrammarFocusCode),
            Array.Empty<TargetWordDto>()) { GrammarFocusCode = topic.GrammarFocusCode };

    private static ErrorCategory GrammarFocus(VocabularyTopic topic) =>
        Models.GrammarFocusCategory.For(topic.GrammarFocusCode);
}

/// <summary>
/// The graded outcome of one exercise, with the correct answer revealed. <see cref="Explanation"/>
/// is the English "why this is correct" note (immersion teaching content) and is populated only for
/// an incorrect answer.
/// </summary>
public sealed record GrammarExerciseOutcomeDto(
    Guid ExerciseId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Explanation);

/// <summary>Side-effect-free feedback for one checked grammar answer.</summary>
public sealed record GrammarExerciseCheckDto(
    Guid ExerciseId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Explanation);

/// <summary>
/// The result of submitting a topic's grammar exercises (PROJECT-SPEC G.2). The score feeds Grammar
/// skill activity (G.4) and the error heatmap (C.7), and is credited toward the topic's Grammar
/// module in its six-module mastery checklist (K.5) - the topic is the catalog key, so
/// <see cref="Completion"/> is always tracked.
/// </summary>
public sealed record GrammarExerciseResultDto(
    Guid TopicId,
    int TotalExercises,
    int CorrectCount,
    int ScorePercent,
    bool Passed,
    IReadOnlyList<GrammarExerciseOutcomeDto> Outcomes,
    TopicCompletionDto? Completion);
