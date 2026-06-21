using Application.Common;
using Application.Learning.Ports;
using Application.Referral.Ports;
using Application.Vocabulary.Ports;
using Domain.Learning;
using Domain.Subscription;
using Domain.Vocabulary;

namespace Application.Subscription.Access;

/// <inheritdoc />
public sealed class TopicAccessPolicy : ITopicAccessPolicy
{
    private readonly IProAccessService _proAccess;
    private readonly IComplimentaryAccess _complimentary;
    private readonly ITopicCompletionStore _completions;
    private readonly IVocabularyTopicRepository _topics;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IReferralStore _referrals;

    public TopicAccessPolicy(
        IProAccessService proAccess,
        IComplimentaryAccess complimentary,
        ITopicCompletionStore completions,
        IVocabularyTopicRepository topics,
        ILearnerProfileRepository profiles,
        IReferralStore referrals)
    {
        _proAccess = proAccess;
        _complimentary = complimentary;
        _completions = completions;
        _topics = topics;
        _profiles = profiles;
        _referrals = referrals;
    }

    public async Task<TopicAccessDecision> EvaluateAsync(
        Guid learnerId, Guid topicId, CancellationToken cancellationToken)
    {
        var hasFullAccess = await HasFullAccessAsync(learnerId, cancellationToken);

        // The free set is the learner's earliest-started topics, so order by when the record began.
        var records = await _completions.GetByLearnerAsync(learnerId, cancellationToken);
        var startedTopicIds = records
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.VocabularyTopicId)
            .ToList();

        // Referral bonus permanently widens the free trial by +2 topics per qualified referral.
        // Skipped for privileged learners (they already have unlimited access).
        var bonusTopics = 0;
        if (!hasFullAccess)
        {
            var account = await _referrals.GetAccountByLearnerAsync(learnerId, cancellationToken);
            bonusTopics = account?.BonusTopicAllowance ?? 0;
        }

        return FreeTopicAccessPolicy.Evaluate(hasFullAccess, startedTopicIds, topicId, bonusTopics);
    }

    public async Task<TopicAccessDecision> EnsureAccessAsync(
        Guid learnerId, Guid topicId, CancellationToken cancellationToken)
    {
        var decision = await EvaluateAsync(learnerId, topicId, cancellationToken);
        if (!decision.IsAllowed)
            throw new SubscriptionRequiredException(decision.FreeAllowance);

        return decision;
    }

    public async Task<TopicAccessDecision> EnsureLearningAccessAsync(
        Guid learnerId,
        Guid topicId,
        SkillType module,
        CancellationToken cancellationToken)
    {
        var decision = await EnsureAccessAsync(learnerId, topicId, cancellationToken);

        // Complimentary access is the explicit progression bypass. Premium removes the commercial
        // paywall, but it does not let a learner skip CEFR bands, topics, or prerequisite skills.
        if (await _complimentary.HasFullAccessAsync(learnerId, cancellationToken))
            return decision;

        var topic = await _topics.GetByIdAsync(topicId, cancellationToken)
            ?? throw new NotFoundException("Vocabulary topic", topicId);
        var profile = await _profiles.GetByLearnerIdAsync(learnerId, cancellationToken);
        var currentLevel = profile?.OverallLevel ?? Domain.Assessment.CefrLevel.A1;

        if (topic.Level > currentLevel)
            throw new TopicLockedException();

        return decision;
    }

    public async Task<bool> HasFullAccessAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        if (await _complimentary.HasFullAccessAsync(learnerId, cancellationToken))
            return true;

        return (await _proAccess.EvaluateAsync(learnerId, cancellationToken)).HasFullAccess;
    }
}
