using Domain.Common;

namespace Domain.Referral;

/// <summary>
/// A learner's referral standing (one per learner). Holds the learner's own shareable
/// <see cref="Code"/> and the bonus balances they have earned by referring friends:
/// permanently unlocked vocabulary topics plus consumable Speaking/Writing credits.
///
/// The rewards are deliberately cheap and capped so the programme can never become a loss
/// centre (the founder's concern about self-referral farming):
/// <list type="bullet">
/// <item>Topics are curated content - unlocking two more costs nothing per use.</item>
/// <item>Speaking/Writing are the only expensive (AI) features, so they are granted as a
/// small, one-time credit pool - not unlimited Premium - and consumed one at a time only
/// after the daily/monthly free allowance is spent.</item>
/// <item><see cref="MaxRewardedReferrals"/> caps the lifetime payout, so twenty throwaway
/// Gmail accounts still yield at most the capped bundle, once.</item>
/// </list>
/// </summary>
public sealed class ReferralAccount
{
    /// <summary>Extra vocabulary topics permanently unlocked per qualified referral.</summary>
    public const int TopicsPerReferral = 2;

    /// <summary>Bonus Speaking sessions granted per qualified referral (consumable pool).</summary>
    public const int SpeakingCreditsPerReferral = 3;

    /// <summary>Bonus Writing assessments granted per qualified referral (consumable pool).</summary>
    public const int WritingCreditsPerReferral = 2;

    /// <summary>Lifetime cap on how many referrals a single learner can be rewarded for.</summary>
    public const int MaxRewardedReferrals = 5;

    // Parameterless ctor for EF Core materialization.
    private ReferralAccount()
    {
        Code = string.Empty;
    }

    private ReferralAccount(Guid id, Guid learnerId, string code, DateTimeOffset now)
    {
        Id = id;
        LearnerId = learnerId;
        Code = code;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }

    /// <summary>The learner's own shareable code (normalized, unique across all accounts).</summary>
    public string Code { get; private set; }

    /// <summary>How many referrals this learner has already been rewarded for (capped).</summary>
    public int RewardedCount { get; private set; }

    /// <summary>Extra vocabulary topics unlocked on top of the free allowance (permanent).</summary>
    public int BonusTopicAllowance { get; private set; }

    /// <summary>Remaining bonus Speaking sessions (consumed after the free allowance).</summary>
    public int RemainingSpeakingCredits { get; private set; }

    /// <summary>Remaining bonus Writing assessments (consumed after the free allowance).</summary>
    public int RemainingWritingCredits { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Whether the lifetime reward cap has been reached (no further payout).</summary>
    public bool HasReachedRewardCap => RewardedCount >= MaxRewardedReferrals;

    /// <summary>Creates a referral account for a learner with a pre-generated unique code.</summary>
    public static ReferralAccount Create(Guid learnerId, string code, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        var normalized = ReferralCode.Normalize(code);
        if (!ReferralCode.IsValid(normalized))
            throw new DomainException("Referral code has an invalid format.");

        return new ReferralAccount(Guid.NewGuid(), learnerId, normalized, now);
    }

    /// <summary>
    /// Grants one referral's reward bundle (topics + Speaking + Writing credits), unless the
    /// lifetime cap is already reached. Returns whether the reward was actually applied so the
    /// caller can stay idempotent and honest about capped payouts.
    /// </summary>
    public bool GrantReferralReward(DateTimeOffset now)
    {
        if (HasReachedRewardCap)
            return false;

        RewardedCount++;
        BonusTopicAllowance += TopicsPerReferral;
        RemainingSpeakingCredits += SpeakingCreditsPerReferral;
        RemainingWritingCredits += WritingCreditsPerReferral;
        UpdatedAt = now;
        return true;
    }

    /// <summary>Consumes one bonus Speaking credit if any remain. Returns whether one was spent.</summary>
    public bool ConsumeSpeakingCredit(DateTimeOffset now)
    {
        if (RemainingSpeakingCredits <= 0)
            return false;

        RemainingSpeakingCredits--;
        UpdatedAt = now;
        return true;
    }

    /// <summary>Consumes one bonus Writing credit if any remain. Returns whether one was spent.</summary>
    public bool ConsumeWritingCredit(DateTimeOffset now)
    {
        if (RemainingWritingCredits <= 0)
            return false;

        RemainingWritingCredits--;
        UpdatedAt = now;
        return true;
    }
}
