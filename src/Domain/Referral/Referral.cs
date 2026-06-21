using Domain.Common;

namespace Domain.Referral;

/// <summary>
/// A single referral link: the record that <see cref="RefereeId"/> (a newly registered learner)
/// signed up using <see cref="ReferrerId"/>'s <see cref="Code"/>. There is at most one referral
/// per referee - a learner can only ever be referred once - which is the primary abuse guard
/// alongside the referrer's lifetime cap on <see cref="ReferralAccount"/>.
///
/// The referral is created <see cref="ReferralStatus.Pending"/> at sign-up and only transitions
/// to <see cref="ReferralStatus.Qualified"/> when the referee first engages (completes a skill),
/// at which moment both parties are rewarded - exactly once.
/// </summary>
public sealed class Referral
{
    // Parameterless ctor for EF Core materialization.
    private Referral()
    {
        Code = string.Empty;
    }

    private Referral(Guid id, Guid referrerId, Guid refereeId, string code, DateTimeOffset now)
    {
        Id = id;
        ReferrerId = referrerId;
        RefereeId = refereeId;
        Code = code;
        Status = ReferralStatus.Pending;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>The learner who shared the code (receives a reward when this qualifies).</summary>
    public Guid ReferrerId { get; private set; }

    /// <summary>The newly registered learner who used the code (also rewarded on qualifying).</summary>
    public Guid RefereeId { get; private set; }

    /// <summary>The code that was used (normalized), kept for auditing/analytics.</summary>
    public string Code { get; private set; }

    public ReferralStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? QualifiedAt { get; private set; }

    /// <summary>
    /// Records that <paramref name="refereeId"/> signed up with <paramref name="referrerId"/>'s
    /// code. Self-referral is rejected in the domain as a last line of defence (the application
    /// also guards it earlier).
    /// </summary>
    public static Referral Create(Guid referrerId, Guid refereeId, string code, DateTimeOffset now)
    {
        if (referrerId == Guid.Empty)
            throw new DomainException("Referrer id must not be empty.");
        if (refereeId == Guid.Empty)
            throw new DomainException("Referee id must not be empty.");
        if (referrerId == refereeId)
            throw new DomainException("A learner cannot refer themselves.");

        var normalized = ReferralCode.Normalize(code);
        if (!ReferralCode.IsValid(normalized))
            throw new DomainException("Referral code has an invalid format.");

        return new Referral(Guid.NewGuid(), referrerId, refereeId, normalized, now);
    }

    /// <summary>
    /// Transitions a pending referral to qualified. Returns whether it actually transitioned so
    /// the reward is granted exactly once (a second call on an already-qualified referral is a
    /// harmless no-op).
    /// </summary>
    public bool MarkQualified(DateTimeOffset now)
    {
        if (Status != ReferralStatus.Pending)
            return false;

        Status = ReferralStatus.Qualified;
        QualifiedAt = now;
        return true;
    }
}
