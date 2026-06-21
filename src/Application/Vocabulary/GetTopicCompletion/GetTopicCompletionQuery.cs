using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.GetTopicCompletion;

/// <summary>
/// Returns a learner's module-by-module mastery progress for a topic (PROJECT-SPEC K.5). Yields a
/// zeroed record (no modules passed) when the learner has not yet attempted the topic, so the UI
/// can always render the six-module checklist.
/// </summary>
public sealed record GetTopicCompletionQuery(Guid LearnerId, Guid TopicId) : IRequest<TopicCompletionDto>;
