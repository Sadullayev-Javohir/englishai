using Application.Vocabulary;
using Application.Vocabulary.Dtos;
using Domain.Assessment;
using Domain.Reading;
using Domain.Vocabulary;

namespace Application.Reading.Dtos;

/// <summary>
/// A catalog row for the reading list. Each row is one learning-spine topic - every skill teaches
/// the same 50 topics per level - so the id here is the topic id, and the reading lesson is
/// generated lazily when the topic is opened.
/// </summary>
public sealed record ReadingSummaryDto(
    Guid TopicId,
    string Title,
    string TitleUz,
    string Category,
    CefrLevel Level)
{
    public static ReadingSummaryDto FromTopic(VocabularyTopic topic) =>
        new(topic.Id, topic.Title, topic.TitleUz, topic.Category, topic.Level);
}

/// <summary>An interactive glossary entry (tap-to-translate) for the reader.</summary>
public sealed record GlossaryEntryDto(string Word, string Translation, string? ExampleSentence)
{
    public static GlossaryEntryDto FromDomain(GlossaryEntry entry) =>
        new(entry.Word, entry.Translation, entry.ExampleSentence);
}

/// <summary>
/// A quiz question as shown to the learner. The correct answer is deliberately withheld here -
/// the front-end reveals it only after the learner submits (via <see cref="ReadingQuestionOutcomeDto"/>),
/// and the authoritative score is computed server-side in <see cref="ReadingQuizResultDto"/>.
/// </summary>
public sealed record ReadingQuestionDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<string> Options)
{
    public static ReadingQuestionDto FromDomain(ReadingQuestion question) =>
        new(question.Id, question.Prompt, question.Options);
}

/// <summary>
/// Full reading-lesson detail keyed by its learning-spine topic: body, interactive glossary and
/// (answerless) quiz. When <see cref="IsReady"/> is false the content is still being generated
/// (the lesson is pending), so the reader shows an honest "preparing" state rather than fake text.
/// </summary>
public sealed record ReadingPassageDto(
    Guid TopicId,
    Guid PassageId,
    string Title,
    string Body,
    string Topic,
    CefrLevel Level,
    int WordCount,
    bool IsReady,
    IReadOnlyList<GlossaryEntryDto> Glossary,
    IReadOnlyList<ReadingQuestionDto> Questions,
    IReadOnlyList<TargetWordDto> TargetWords)
{
    public static ReadingPassageDto FromDomain(VocabularyTopic topic, ReadingPassage passage) =>
        new(topic.Id, passage.Id, passage.Title, passage.Body, passage.Topic, passage.Level,
            passage.WordCount, passage.IsFilled,
            passage.Glossary.Select(GlossaryEntryDto.FromDomain).ToList(),
            passage.Questions.Select(ReadingQuestionDto.FromDomain).ToList(),
            TopicTargetWords.DetailedOf(topic));

    /// <summary>A pending placeholder for a topic whose reading lesson is not generated yet.</summary>
    public static ReadingPassageDto Pending(VocabularyTopic topic) =>
        new(topic.Id, Guid.Empty, topic.Title, string.Empty, topic.Category, topic.Level,
            0, false, Array.Empty<GlossaryEntryDto>(), Array.Empty<ReadingQuestionDto>(),
            Array.Empty<TargetWordDto>());
}

/// <summary>
/// The graded outcome of one question, with the correct answer revealed. <see cref="Explanation"/>
/// is the English "why this is correct" note (immersion teaching content) and is populated only for
/// an incorrect answer.
/// </summary>
public sealed record ReadingQuestionOutcomeDto(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Explanation);

/// <summary>
/// Side-effect-free result for checking one answer during the quiz. The final batch submission remains
/// authoritative for the score, learner activity, rewards and topic completion.
/// </summary>
public sealed record ReadingAnswerCheckDto(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Explanation);

/// <summary>
/// The result of submitting a reading comprehension quiz (PROJECT-SPEC Faza 6). When the learner
/// is known, the score is credited toward the topic's Reading module (K.5) and the updated
/// six-module checklist is returned in <see cref="Completion"/>.
/// </summary>
public sealed record ReadingQuizResultDto(
    Guid TopicId,
    int TotalQuestions,
    int CorrectCount,
    int ScorePercent,
    bool Passed,
    IReadOnlyList<ReadingQuestionOutcomeDto> Outcomes,
    TopicCompletionDto? Completion);
