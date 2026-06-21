namespace Domain.Subscription;

/// <summary>Lifecycle state of a learner's subscription (PROJECT-SPEC Qism H).</summary>
public enum SubscriptionStatus
{
    /// <summary>Default free tier with feature limits (PROJECT-SPEC H.1).</summary>
    Free = 0,

    /// <summary>Paid tier, currently active.</summary>
    Premium = 1,

    /// <summary>Was Premium, but the paid period has elapsed.</summary>
    Expired = 2,

    /// <summary>User cancelled auto-renew; access remains until the paid period ends.</summary>
    Cancelled = 3,
}

/// <summary>The billing period a learner buys (PROJECT-SPEC H.1).</summary>
public enum SubscriptionPlan
{
    Monthly = 0,
    Yearly = 1,
    // Added after Monthly/Yearly: keep existing numeric values stable for persisted rows.
    Quarterly = 2,
    SemiAnnual = 3,
}

/// <summary>State of a single payment attempt.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
}

/// <summary>Payment provider (PROJECT-SPEC H.3). Local is the dev/test gateway.</summary>
public enum PaymentProvider
{
    Local = 0,
    Click = 1,
    Payme = 2,
    Uzum = 3,
}
