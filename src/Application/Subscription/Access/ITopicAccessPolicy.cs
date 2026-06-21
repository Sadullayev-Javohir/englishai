using Domain.Learning;
using Domain.Subscription;

namespace Application.Subscription.Access;

/// <summary>
/// Application service that enforces the topic trial paywall (PROJECT-SPEC H.1). It resolves the
/// learner's privilege (active Premium subscription or complimentary allowlist) and how many topics
/// they have already started, then defers to the pure <see cref="FreeTopicAccessPolicy"/>. Every
/// topic-scoped entry point (each skill's content query, the speaking start, and the module-score
/// commands) depends on this so the paywall is enforced server-side and cannot be bypassed by calling
/// the API directly.
/// </summary>
public interface ITopicAccessPolicy
{
    /// <summary>Evaluates whether the learner may access the topic, without throwing.</summary>
    Task<TopicAccessDecision> EvaluateAsync(Guid learnerId, Guid topicId, CancellationToken cancellationToken);

    /// <summary>
    /// Throws <see cref="Common.SubscriptionRequiredException"/> when the topic is beyond the learner's
    /// free trial; otherwise returns the decision so the caller can proceed.
    /// </summary>
    Task<TopicAccessDecision> EnsureAccessAsync(Guid learnerId, Guid topicId, CancellationToken cancellationToken);

    /// <summary>
    /// Enforces the subscription decision and the learner's CEFR progression gate.
    /// Topics and skills within the current or lower placement bands remain freely selectable.
    /// </summary>
    Task<TopicAccessDecision> EnsureLearningAccessAsync(
        Guid learnerId,
        Guid topicId,
        SkillType module,
        CancellationToken cancellationToken);

    /// <summary>
    /// Whether the learner has unlimited topic access - an active Premium subscription or a
    /// complimentary-allowlist account. Used by the topic-list handlers to flag paywalled topics
    /// without a per-topic round-trip.
    /// </summary>
    Task<bool> HasFullAccessAsync(Guid learnerId, CancellationToken cancellationToken);
}
