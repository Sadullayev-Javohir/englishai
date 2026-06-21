using Application.Common;
using Application.Referral.Ports;
using Application.Subscription.Access;
using Application.Subscription.Ports;
using Domain.Subscription;

namespace Application.Subscription.Entitlements;

/// <inheritdoc />
public sealed class EntitlementService : IEntitlementService
{
    private readonly IProAccessService _proAccess;
    private readonly IUsageCounter _usage;
    private readonly IReferralStore _referrals;
    private readonly TimeProvider _clock;

    public EntitlementService(
        IProAccessService proAccess,
        IUsageCounter usage,
        IReferralStore referrals,
        TimeProvider clock)
    {
        _proAccess = proAccess;
        _usage = usage;
        _referrals = referrals;
        _clock = clock;
    }

    public async Task<GateDecision> EvaluateAsync(
        Guid learnerId, PremiumFeature feature, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var isPremium = await IsPremiumAsync(learnerId, now, cancellationToken);
        var used = await _usage.GetCountAsync(learnerId, feature, PeriodKey(feature, now), cancellationToken);
        var decision = EntitlementPolicy.Evaluate(feature, isPremium, used);
        if (decision.IsAllowed)
            return decision;

        // Free allowance is spent - a referral bonus credit (Speaking/Writing) can still cover this
        // one use. Topics are gated elsewhere; NewVocabularyWord has no bonus pool.
        if (await HasBonusCreditAsync(learnerId, feature, cancellationToken))
            return decision with { IsAllowed = true };

        return decision;
    }

    public async Task<GateDecision> EnsureAllowedAsync(
        Guid learnerId, PremiumFeature feature, CancellationToken cancellationToken)
    {
        var decision = await EvaluateAsync(learnerId, feature, cancellationToken);
        if (!decision.IsAllowed)
            throw new FeatureLimitExceededException(feature, decision.Limit, decision.Period);

        return decision;
    }

    public async Task RecordUsageAsync(Guid learnerId, PremiumFeature feature, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var isPremium = await IsPremiumAsync(learnerId, now, cancellationToken);
        var used = await _usage.GetCountAsync(learnerId, feature, PeriodKey(feature, now), cancellationToken);
        var freeLimit = EntitlementPolicy.FreeLimitFor(feature);

        // Within the free allowance (or Premium/comped, which is unlimited) the usage is metered by
        // the period counter as before. Only once the free allowance is exhausted do we draw down a
        // referral bonus credit - so bonuses top up the free tier rather than replace it.
        if (isPremium || used < freeLimit.MaxUses)
        {
            await _usage.IncrementAsync(learnerId, feature, PeriodKey(feature, now), cancellationToken);
            return;
        }

        if (await TryConsumeBonusCreditAsync(learnerId, feature, now, cancellationToken))
            return;

        // No bonus available (shouldn't happen when gated by EnsureAllowedAsync) - fall back to the
        // period counter so usage is never silently uncounted.
        await _usage.IncrementAsync(learnerId, feature, PeriodKey(feature, now), cancellationToken);
    }

    private async Task<bool> HasBonusCreditAsync(
        Guid learnerId, PremiumFeature feature, CancellationToken cancellationToken)
    {
        if (!HasBonusPool(feature))
            return false;

        var account = await _referrals.GetAccountByLearnerAsync(learnerId, cancellationToken);
        if (account is null)
            return false;

        return feature switch
        {
            PremiumFeature.SpeakingSession => account.RemainingSpeakingCredits > 0,
            PremiumFeature.WritingAssessment => account.RemainingWritingCredits > 0,
            _ => false,
        };
    }

    private async Task<bool> TryConsumeBonusCreditAsync(
        Guid learnerId, PremiumFeature feature, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!HasBonusPool(feature))
            return false;

        var account = await _referrals.GetAccountByLearnerAsync(learnerId, cancellationToken);
        if (account is null)
            return false;

        var consumed = feature switch
        {
            PremiumFeature.SpeakingSession => account.ConsumeSpeakingCredit(now),
            PremiumFeature.WritingAssessment => account.ConsumeWritingCredit(now),
            _ => false,
        };

        if (consumed)
            await _referrals.SaveAccountAsync(account, cancellationToken);

        return consumed;
    }

    // Only the expensive AI features carry a referral bonus pool.
    private static bool HasBonusPool(PremiumFeature feature) =>
        feature is PremiumFeature.SpeakingSession or PremiumFeature.WritingAssessment;

    private async Task<bool> IsPremiumAsync(Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken) =>
        (await _proAccess.EvaluateAsync(learnerId, cancellationToken)).HasFullAccess;

    // A stable key per (feature, period) window so daily/monthly counters reset naturally. Shared with
    // the metered allowances so every quota in the app agrees on when "today" starts.
    private static string PeriodKey(PremiumFeature feature, DateTimeOffset now) =>
        UsagePeriodKey.For(feature, now);
}
