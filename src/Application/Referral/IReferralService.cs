using Domain.Referral;

namespace Application.Referral;

/// <summary>
/// Application service that owns the referral programme's write-side rules: minting each
/// learner's unique code, recording who referred whom at sign-up, and - the moment a referred
/// learner genuinely engages - qualifying the referral and paying the capped reward bundle to
/// both sides exactly once. Command handlers depend on this abstraction so they stay thin and
/// testable (docs/development-guide.md rule 10, 17).
/// </summary>
public interface IReferralService
{
    /// <summary>
    /// Returns the learner's referral account, creating one (with a freshly minted unique code)
    /// on first access. Idempotent - subsequent calls return the same account.
    /// </summary>
    Task<ReferralAccount> GetOrCreateAccountAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Records that a brand-new learner signed up with someone's code and immediately pays the
    /// capped reward bundle (+2 topics plus the small Speaking/Writing credit pool) to BOTH the
    /// referrer and the new learner. No-op (silently ignored) when the code is blank, malformed,
    /// unknown, or the learner's own - a bad code must never break sign-up. At most one referral
    /// is ever recorded (and rewarded) per referee.
    /// </summary>
    Task CaptureAsync(Guid refereeId, string? code, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Legacy engagement hook, kept so callers on the skill-completion path stay valid. Rewards
    /// are now granted up-front in <see cref="CaptureAsync"/>, so for a normal sign-up this is a
    /// harmless no-op (the referral is already qualified). It still qualifies and pays any
    /// referral that is somehow still pending, staying idempotent - the reward is paid once.
    /// </summary>
    Task TryQualifyAsync(Guid refereeId, DateTimeOffset now, CancellationToken cancellationToken);
}
