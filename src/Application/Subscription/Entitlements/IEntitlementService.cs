using Domain.Subscription;

namespace Application.Subscription.Entitlements;

/// <summary>
/// Application service that enforces freemium gating (PROJECT-SPEC H.1). It combines the
/// learner's tier (subscription) with their period usage and the pure
/// <see cref="EntitlementPolicy"/> to decide access, and records usage when a metered
/// action succeeds. Command handlers depend on this abstraction so they stay testable.
/// </summary>
public interface IEntitlementService
{
    /// <summary>Evaluates whether the learner may use the feature once more, without recording usage.</summary>
    Task<GateDecision> EvaluateAsync(Guid learnerId, PremiumFeature feature, CancellationToken cancellationToken);

    /// <summary>
    /// Throws <see cref="Common.FeatureLimitExceededException"/> if the feature is not
    /// currently allowed; otherwise returns the decision so the caller can proceed.
    /// </summary>
    Task<GateDecision> EnsureAllowedAsync(Guid learnerId, PremiumFeature feature, CancellationToken cancellationToken);

    /// <summary>Records one use of the feature against the current period.</summary>
    Task RecordUsageAsync(Guid learnerId, PremiumFeature feature, CancellationToken cancellationToken);
}
