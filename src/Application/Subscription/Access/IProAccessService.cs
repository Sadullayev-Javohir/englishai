namespace Application.Subscription.Access;

public sealed record ProAccessDecision(
    bool HasFullAccess,
    bool IsPaidPremium,
    bool IsComplimentary,
    bool IsTrialActive,
    DateTimeOffset? TrialExpiresAt);

public interface IProAccessService
{
    Task<ProAccessDecision> EvaluateAsync(Guid learnerId, CancellationToken cancellationToken);
}
