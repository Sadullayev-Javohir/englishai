using Application.Vocabulary.Dtos;
using Domain.Learning;
using MediatR;

namespace Application.Vocabulary.ResetTopicModuleScore;

public sealed record ResetTopicModuleScoreCommand(
    Guid LearnerId, Guid TopicId, SkillType Module) : IRequest<TopicCompletionDto>;
