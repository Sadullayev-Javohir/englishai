using Application.Common;
using Application.Identity.Ports;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Common;
using Domain.Identity;
using Domain.Learning;
using MediatR;

namespace Application.Learning.GetRecommendedTopics;

public sealed class GetRecommendedTopicsQueryHandler
    : IRequestHandler<GetRecommendedTopicsQuery, RecommendedTopicsDto>
{
    /// <summary>Level used when the learner has no profile yet (mirrors GetLevelMap's default).</summary>
    private const CefrLevel DefaultLevel = CefrLevel.A2;

    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IVocabularyTopicRepository _topics;

    public GetRecommendedTopicsQueryHandler(
        IUserAccountStore accounts,
        ILearnerProfileRepository profiles,
        IVocabularyTopicRepository topics)
    {
        _accounts = accounts;
        _profiles = profiles;
        _topics = topics;
    }

    public async Task<RecommendedTopicsDto> Handle(
        GetRecommendedTopicsQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.LearnerId);

        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);

        // The goal lives on the learner profile now; before onboarding (no profile) it falls back to
        // Unspecified, which yields the plain catalog order - nothing regresses.
        var goal = profile?.LearningGoal ?? LearningGoal.Unspecified;
        var level = profile?.OverallLevel ?? DefaultLevel;

        var topics = await _topics.GetByLevelAsync(level, cancellationToken);

        // Order by goal relevance (heaviest first), then by the topic's own learning Sequence so the
        // result is deterministic and, for Unspecified, identical to the plain catalog order.
        var ordered = topics
            .OrderByDescending(t => LearningGoalAffinity.Weight(goal, t.Category))
            .ThenBy(t => t.Sequence)
            .Take(request.Count)
            .Select(t => new RecommendedTopicDto(
                t.Id,
                t.Title,
                t.TitleUz,
                t.Category,
                t.Level,
                LearningGoalAffinity.IsRelevant(goal, t.Category)))
            .ToList();

        return new RecommendedTopicsDto(goal, level, ordered);
    }
}
