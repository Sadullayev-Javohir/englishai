using Application.Reading.Dtos;
using MediatR;

namespace Application.Reading.SubmitReadingQuiz;

/// <summary>One answer in a reading quiz submission.</summary>
public sealed record ReadingQuizAnswer(Guid QuestionId, int SelectedOptionIndex);

/// <summary>
/// Submits a learner's answers to a topic's reading comprehension quiz for server-side grading
/// (PROJECT-SPEC Faza 6). The score is recorded as Reading skill activity (G.4, when the learner
/// has a profile) and credited toward the topic's Reading module in its six-module mastery
/// checklist (K.5) - the topic is the catalog key, so completion is always tracked.
/// </summary>
public sealed record SubmitReadingQuizCommand(
    Guid TopicId,
    Guid LearnerId,
    IReadOnlyList<ReadingQuizAnswer> Answers) : IRequest<ReadingQuizResultDto>;
