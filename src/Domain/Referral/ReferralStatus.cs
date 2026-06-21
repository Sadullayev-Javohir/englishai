namespace Domain.Referral;

/// <summary>
/// Lifecycle of a single referral (one referred friend). A referral starts <see cref="Pending"/>
/// the moment a new account signs up with someone's code, and becomes <see cref="Qualified"/>
/// only once the referred learner shows genuine engagement (completes their first skill). The
/// reward is granted exactly once, on that qualifying transition, so fake sign-ups that never
/// study earn nothing.
/// </summary>
public enum ReferralStatus
{
    /// <summary>The friend signed up with a code but has not yet qualified - no reward given.</summary>
    Pending = 0,

    /// <summary>The friend engaged genuinely; both sides have been rewarded. Terminal.</summary>
    Qualified = 1,
}
