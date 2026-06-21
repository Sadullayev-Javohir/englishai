using Domain.Grammar;

namespace Application.Grammar.Admin;

public sealed record GrammarLessonAdminDetailDto(
    Guid Id,
    string Title,
    string Category,
    string Level,
    string Status,
    Guid? VocabularyTopicId,
    string? GrammarFocusCode,
    string ContextIntro,
    string? Explanation,
    string? CuratedTitleUz,
    string? CuratedSummaryUz,
    IReadOnlyList<string> CuratedFormulas,
    IReadOnlyList<GrammarCuratedRuleAdminDto> CuratedRules,
    IReadOnlyList<GrammarExampleAdminDto> Examples,
    IReadOnlyList<GrammarCommonMistakeAdminDto> CommonMistakes,
    IReadOnlyList<GrammarExerciseAdminDto> Exercises,
    IReadOnlyList<GrammarApplicationTaskAdminDto> ApplicationTasks,
    DateTimeOffset CreatedAt)
{
    public static GrammarLessonAdminDetailDto FromDomain(GrammarLesson lesson) => new(
        lesson.Id, lesson.Topic, lesson.Category.ToString(), lesson.Level.ToString(),
        lesson.Status.ToString(), lesson.VocabularyTopicId, lesson.GrammarFocusCode,
        lesson.ContextIntro, lesson.Explanation,
        lesson.CuratedTitleUz, lesson.CuratedSummaryUz, lesson.CuratedFormulas,
        lesson.CuratedRules.Select(item => new GrammarCuratedRuleAdminDto(item.HeadingUz, item.BodyUz)).ToArray(),
        lesson.Examples.Select(item => new GrammarExampleAdminDto(item.English, item.Uzbek)).ToArray(),
        lesson.CommonMistakes.Select(item => new GrammarCommonMistakeAdminDto(item.Text)).ToArray(),
        lesson.Exercises.Select(item => new GrammarExerciseAdminDto(
            item.Type.ToString(), item.Prompt, item.Options, item.CorrectOptionIndex,
            item.HintCode, item.Explanation)).ToArray(),
        lesson.ApplicationTasks.Select(item => new GrammarApplicationTaskAdminDto(
            item.TargetSkill.ToString(), item.Prompt)).ToArray(),
        lesson.CreatedAt);
}

public sealed record GrammarExampleAdminDto(string English, string Uzbek);
public sealed record GrammarCommonMistakeAdminDto(string Text);
public sealed record GrammarExerciseAdminDto(string Type, string Prompt, IReadOnlyList<string> Options, int CorrectOptionIndex, string? HintCode, string? Explanation);
public sealed record GrammarApplicationTaskAdminDto(string TargetSkill, string Prompt);
public sealed record GrammarCuratedRuleAdminDto(string HeadingUz, string BodyUz);

public sealed record GrammarLessonAdminFullUpdateDto(
    string Title,
    string Category,
    string Level,
    string Status,
    Guid? VocabularyTopicId,
    string? GrammarFocusCode,
    string ContextIntro,
    string? Explanation,
    string? CuratedTitleUz,
    string? CuratedSummaryUz,
    IReadOnlyList<string> CuratedFormulas,
    IReadOnlyList<GrammarCuratedRuleAdminDto> CuratedRules,
    IReadOnlyList<GrammarExampleAdminDto> Examples,
    IReadOnlyList<GrammarCommonMistakeAdminDto> CommonMistakes,
    IReadOnlyList<GrammarExerciseAdminDto> Exercises,
    IReadOnlyList<GrammarApplicationTaskAdminDto> ApplicationTasks);
