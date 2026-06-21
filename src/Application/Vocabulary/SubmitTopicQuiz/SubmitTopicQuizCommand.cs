using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.SubmitTopicQuiz;

/// <summary>
/// Grades a learner's answers to a topic's cloze quiz (PROJECT-SPEC B.1 retrieval practice).
/// <paramref name="Answers"/> maps a question index to the chosen option index. The quiz is
/// re-derived from the topic's stored words (deterministic), so no quiz state is persisted.
/// When <paramref name="LearnerId"/> is supplied, the quiz percentage is recorded as the
/// Vocabulary module's score toward mastering the topic (PROJECT-SPEC K.5); anonymous grading
/// (no learner) leaves the completion record untouched.
/// </summary>
public sealed record SubmitTopicQuizCommand(
    Guid TopicId, IReadOnlyDictionary<int, int> Answers, Guid? LearnerId = null)
    : IRequest<TopicQuizResultDto>;
