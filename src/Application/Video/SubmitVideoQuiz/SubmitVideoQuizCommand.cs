using Application.Video.Dtos;
using MediatR;

namespace Application.Video.SubmitVideoQuiz;

/// <summary>One answer in a quiz submission: the question and the chosen option index.</summary>
public sealed record QuizAnswer(Guid QuestionId, int SelectedOptionIndex);

/// <summary>
/// Submits a learner's answers to a video comprehension quiz for server-side grading
/// (PROJECT-SPEC Faza 4).
/// </summary>
public sealed record SubmitVideoQuizCommand(
    Guid VideoLessonId,
    Guid LearnerId,
    IReadOnlyList<QuizAnswer> Answers,
    Guid? QuizId = null) : IRequest<VideoQuizResultDto>;
