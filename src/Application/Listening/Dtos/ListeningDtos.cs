using Application.Vocabulary;
using Application.Vocabulary.Dtos;
using Domain.Assessment;
using Domain.Listening;
using Domain.Vocabulary;

namespace Application.Listening.Dtos;

/// <summary>
/// A catalog row for the listening list. Each row is one learning-spine topic - every skill teaches
/// the same 50 topics per level - so the id here is the topic id, and the listening exercise (the
/// topic's audio + comprehension quiz) is generated lazily when the topic is opened.
/// </summary>
public sealed record ListeningSummaryDto(
    Guid TopicId,
    string Title,
    string TitleUz,
    string Category,
    CefrLevel Level)
{
    public static ListeningSummaryDto FromTopic(VocabularyTopic topic) =>
        new(topic.Id, topic.Title, topic.TitleUz, topic.Category, topic.Level);
}

/// <summary>
/// A quiz question as shown to the learner - the correct answer is intentionally omitted so
/// grading happens server-side (revealed only in the <see cref="ListeningQuizResultDto"/>).
/// </summary>
public sealed record ListeningQuestionDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<string> Options)
{
    public static ListeningQuestionDto FromDomain(ListeningQuestion question) =>
        new(question.Id, question.Prompt, question.Options);
}

/// <summary>
/// Full exercise detail for the player keyed by its learning-spine topic: the (answerless) quiz and
/// the transcript. The audio is streamed from the audio endpoint; the transcript is included so the
/// UI can reveal it after the learner answers. When <see cref="IsReady"/> is false the content is
/// still being generated (the exercise is pending), so the player shows an honest "preparing" state
/// rather than fake text (rules 8, 11).
/// </summary>
public sealed record ListeningExerciseDto(
    Guid TopicId,
    string Title,
    string Topic,
    CefrLevel Level,
    int WordCount,
    bool IsReady,
    string Transcript,
    IReadOnlyList<ListeningQuestionDto> Questions,
    IReadOnlyList<TargetWordDto> TargetWords)
{
    public static ListeningExerciseDto FromDomain(VocabularyTopic topic, ListeningExercise exercise) =>
        new(topic.Id, exercise.Title, exercise.Topic, exercise.Level, exercise.WordCount,
            exercise.IsFilled, exercise.Transcript,
            exercise.Questions.Select(ListeningQuestionDto.FromDomain).ToList(),
            TopicTargetWords.DetailedOf(topic));

    /// <summary>A pending placeholder for a topic whose listening exercise is not generated yet.</summary>
    public static ListeningExerciseDto Pending(VocabularyTopic topic) =>
        new(topic.Id, topic.Title, topic.Category, topic.Level, 0, false,
            string.Empty, Array.Empty<ListeningQuestionDto>(), Array.Empty<TargetWordDto>());
}

/// <summary>
/// The graded outcome of one question, with the correct answer revealed. <see cref="Hint"/> holds
/// the "why" note shown only for an incorrect answer - the vetted Uzbek hint (seeded path, rule 11)
/// or the English explanation (generated immersion path), whichever the question carries.
/// </summary>
public sealed record ListeningQuestionOutcomeDto(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Hint);

/// <summary>Side-effect-free feedback for one checked listening answer.</summary>
public sealed record ListeningAnswerCheckDto(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Hint);

/// <summary>
/// The result of submitting a listening comprehension quiz (PROJECT-SPEC Faza 4). When the learner
/// is known, the score is credited toward the topic's Listening module (K.5) and the updated
/// six-module checklist is returned in <see cref="Completion"/>.
/// </summary>
public sealed record ListeningQuizResultDto(
    Guid TopicId,
    int TotalQuestions,
    int CorrectCount,
    int ScorePercent,
    bool Passed,
    IReadOnlyList<ListeningQuestionOutcomeDto> Outcomes,
    TopicCompletionDto? Completion);

/// <summary>Synthesized clip audio plus its detected MIME type, for the audio endpoint.</summary>
public sealed record ListeningAudioResult(
    byte[]? Audio,
    string ContentType,
    DateTimeOffset CreatedAt,
    string? PublicUrl = null,
    string? ETag = null,
    long? SizeBytes = null);
