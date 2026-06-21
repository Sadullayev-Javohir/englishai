using Application.Common;
using Application.Identity.Ports;
using Application.Subscription.Ports;

namespace Application.Subscription.Access;

public sealed class ProAccessService : IProAccessService
{
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IComplimentaryAccess _complimentary;
    private readonly IUserAccountStore _accounts;
    private readonly TimeProvider _clock;
    private readonly IPaymentAvailability? _payments;
    private readonly LegacyAccessOptions _legacyAccess;

    public ProAccessService(
        ISubscriptionRepository subscriptions,
        IComplimentaryAccess complimentary,
        IUserAccountStore accounts,
        TimeProvider clock,
        IPaymentAvailability? payments = null,
        LegacyAccessOptions? legacyAccess = null)
    {
        _subscriptions = subscriptions;
        _complimentary = complimentary;
        _accounts = accounts;
        _clock = clock;
        _payments = payments;
        _legacyAccess = legacyAccess ?? new LegacyAccessOptions();
    }

    public async Task<ProAccessDecision> EvaluateAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var isComplimentary = await _complimentary.HasFullAccessAsync(learnerId, cancellationToken);
        var subscription = await _subscriptions.GetByLearnerIdAsync(learnerId, cancellationToken);
        var isPaidPremium = subscription?.IsPremiumActive(now) ?? false;
        var account = await _accounts.GetByIdAsync(learnerId, cancellationToken);
        var isTrialActive = account?.IsProTrialActive(now) ?? false;

        // Early learners keep what they signed up for until there is a way to pay for it. Retires
        // itself the day checkout opens - see LegacyAccessOptions.
        var isLegacy = account is not null
            && _payments?.PaymentsEnabled == false
            && _legacyAccess.Applies(account.CreatedAt);

        return new ProAccessDecision(
            isComplimentary || isPaidPremium || isTrialActive || isLegacy,
            isPaidPremium,
            // Reported as complimentary: from the learner's side it is the same thing - full access
            // they are not paying for - and the subscription DTO already knows how to render that.
            isComplimentary || isLegacy,
            isTrialActive,
            account?.ProTrialExpiresAt);
    }
}
