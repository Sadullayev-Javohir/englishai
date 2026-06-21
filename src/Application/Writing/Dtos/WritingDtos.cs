using Application.Vocabulary;
using Application.Vocabulary.Dtos;
using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using Domain.Writing;

namespace Application.Writing.Dtos;

/// <summary>
/// A catalog row for the writing list. Each row is one learning-spine topic - every skill teaches
/// the same 50 topics per level - so the id here is the topic id, and the writing task is generated
/// lazily when the topic is opened.
/// </summary>
public sealed record WritingTaskSummaryDto(
    Guid TopicId,
    string Title,
    string TitleUz,
    string Category,
    CefrLevel Level)
{
    public static WritingTaskSummaryDto FromTopic(VocabularyTopic topic) =>
        new(topic.Id, topic.Title, topic.TitleUz, topic.Category, topic.Level);
}

/// <summary>
/// Full writing-task detail keyed by its learning-spine topic: the prompt, English guidance hints
/// and the expected word range. When <see cref="IsReady"/> is false the prompt is still being
/// generated (the task is pending), so the editor shows an honest "preparing" state rather than a
/// fake prompt (rules 8, 11).
/// </summary>
public sealed record WritingTaskDto(
    Guid TopicId,
    Guid TaskId,
    string Title,
    string Prompt,
    CefrLevel Level,
    int MinWords,
    int MaxWords,
    bool IsReady,
    IReadOnlyList<string> Guidance,
    IReadOnlyList<TargetWordDto> TargetWords)
{
    public static WritingTaskDto FromDomain(VocabularyTopic topic, WritingTask task) =>
        new(topic.Id, task.Id, topic.Title, task.Prompt, task.Level, task.MinWords, task.MaxWords,
            task.IsFilled, task.Guidance, TopicTargetWords.DetailedOf(topic));

    /// <summary>A pending placeholder for a topic whose writing task is not generated yet.</summary>
    public static WritingTaskDto Pending(VocabularyTopic topic, int minWords, int maxWords) =>
        new(topic.Id, Guid.Empty, topic.Title, string.Empty, topic.Level, minWords, maxWords,
            false, Array.Empty<string>(), Array.Empty<TargetWordDto>());
}

/// <summary>The 1-5 score for one assessment dimension.</summary>
public sealed record DimensionScoreDto(WritingDimension Dimension, int Score)
{
    public static DimensionScoreDto FromDomain(DimensionScore score) =>
        new(score.Dimension, score.Score);
}

/// <summary>
/// A located issue with its vetted Uzbek explanation already resolved (rule 11). The text
/// span (<see cref="StartOffset"/>/<see cref="EndOffset"/>) lets the UI highlight the place.
/// </summary>
public sealed record WritingIssueDto(
    WritingDimension Dimension,
    int StartOffset,
    int EndOffset,
    ErrorCategory? Category,
    string Explanation);

/// <summary>
/// The full result of assessing a writing submission (PROJECT-SPEC G.3): per-dimension
/// scores, the located issues with Uzbek explanations, the overall band/percent and the
/// estimated CEFR level. <see cref="Completion"/> is the topic's six-module mastery progress
/// after crediting this Writing score (K.5).
/// </summary>
public sealed record WritingAssessmentDto(
    Guid TopicId,
    Guid TaskId,
    IReadOnlyList<DimensionScoreDto> DimensionScores,
    IReadOnlyList<WritingIssueDto> Issues,
    double OverallBand,
    int OverallPercent,
    CefrLevel EstimatedLevel,
    WritingAssessmentSource AssessmentSource,
    TopicCompletionDto? Completion);
