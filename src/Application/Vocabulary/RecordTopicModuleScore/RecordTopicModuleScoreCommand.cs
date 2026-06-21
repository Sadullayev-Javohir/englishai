using Application.Gamification.Dtos;
using Application.Vocabulary.Dtos;
using Domain.Learning;
using MediatR;

namespace Application.Vocabulary.RecordTopicModuleScore;

/// <summary>
/// Records the learner's score for one module of a topic toward mastering it across all six modules
/// (PROJECT-SPEC K.5). The store keeps the best score per module; the topic becomes mastered once
/// every module passes the threshold. This is the integration seam each module's completion flow
/// calls (the Vocabulary quiz wires it directly; the Web endpoint exposes it for the rest).
/// </summary>
public sealed record RecordTopicModuleScoreCommand(
    Guid LearnerId, Guid TopicId, SkillType Module, int Score)
    : IRequest<RecordTopicModuleScoreResult>;

/// <summary>The updated completion record plus whether this score was the one that mastered the topic.</summary>
public sealed record RecordTopicModuleScoreResult(
    TopicCompletionDto Completion, bool JustMastered, SkillRewardDto Reward);
