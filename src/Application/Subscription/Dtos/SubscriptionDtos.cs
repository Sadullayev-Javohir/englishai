using Domain.Subscription;

namespace Application.Subscription.Dtos;

/// <summary>The learner's current subscription state for the Profile/Settings screen.</summary>
public sealed record SubscriptionDto(
    Guid LearnerId,
    SubscriptionStatus Status,
    SubscriptionPlan? Plan,
    DateTimeOffset? ExpiresAt,
    int? DaysUntilExpiry,
    bool IsPremiumActive,
    bool IsTrialActive,
    DateTimeOffset? TrialExpiresAt,
    int? TrialDaysUntilExpiry)
{
    public static SubscriptionDto From(Domain.Subscription.Subscription subscription, DateTimeOffset now) =>
        new(
            subscription.LearnerId,
            subscription.Status,
            subscription.Plan,
            subscription.ExpiresAt,
            subscription.DaysUntilExpiry(now),
            subscription.IsPremiumActive(now),
            IsTrialActive: false,
            TrialExpiresAt: null,
            TrialDaysUntilExpiry: null);

    public static SubscriptionDto Trial(
        Guid learnerId, SubscriptionStatus status, DateTimeOffset expiresAt, DateTimeOffset now) =>
        new(
            learnerId,
            status,
            Plan: null,
            ExpiresAt: null,
            DaysUntilExpiry: null,
            IsPremiumActive: false,
            IsTrialActive: true,
            TrialExpiresAt: expiresAt,
            TrialDaysUntilExpiry: Math.Max(0, (int)Math.Ceiling((expiresAt - now).TotalDays)));

    /// <summary>
    /// The subscription view for a comped account (<see cref="Application.Common.IComplimentaryAccess"/>):
    /// reported as active Premium with no expiry so the UI hides every upgrade prompt and lock,
    /// without persisting a paid subscription row.
    /// </summary>
    public static SubscriptionDto Complimentary(Guid learnerId) =>
        new(learnerId, SubscriptionStatus.Premium, SubscriptionPlan.Yearly, ExpiresAt: null,
            DaysUntilExpiry: null, IsPremiumActive: true, IsTrialActive: false,
            TrialExpiresAt: null, TrialDaysUntilExpiry: null);
}

/// <summary>Checkout details returned when a subscription purchase is started.</summary>
public sealed record StartSubscriptionResultDto(
    string TransactionId,
    string CheckoutUrl,
    int AmountUzs,
    PaymentProvider Provider,
    SubscriptionPlan Plan);
