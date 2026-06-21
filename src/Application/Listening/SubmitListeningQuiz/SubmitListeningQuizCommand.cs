using Application.Listening.Dtos;
using MediatR;

namespace Application.Listening.SubmitListeningQuiz;

/// <summary>One answer in a listening quiz submission.</summary>
public sealed record ListeningQuizAnswer(Guid QuestionId, int SelectedOptionIndex);

/// <summary>
/// Submits a learner's answers to a topic's listening comprehension quiz for server-side grading
/// (PROJECT-SPEC Faza 4), keyed by the learning-spine <see cref="TopicId"/>. When a
/// <see cref="LearnerId"/> with an existing profile is given, the score is recorded as Listening
/// skill activity (G.4); the score is also credited toward the topic's Listening module (K.5).
/// </summary>
public sealed record SubmitListeningQuizCommand(
    Guid TopicId,
    Guid LearnerId,
    IReadOnlyList<ListeningQuizAnswer> Answers) : IRequest<ListeningQuizResultDto>;
