namespace Application.Referral.Dtos;

/// <summary>
/// The learner-facing snapshot of their referral standing, shown in the Profile "Invite friends"
/// section. Combines the shareable code with progress (how many friends joined / qualified) and
/// the bonus currently in hand, so the UI can render everything in one call.
/// </summary>
/// <param name="Code">The learner's own shareable referral code.</param>
/// <param name="InvitedCount">Friends who signed up with the code (pending + qualified).</param>
/// <param name="QualifiedCount">Friends who engaged and triggered a reward.</param>
/// <param name="RewardCap">Lifetime cap on rewarded referrals (for a progress bar).</param>
/// <param name="BonusTopicsUnlocked">Extra vocabulary topics earned (permanent).</param>
/// <param name="RemainingSpeakingCredits">Bonus Speaking sessions still available.</param>
/// <param name="RemainingWritingCredits">Bonus Writing assessments still available.</param>
/// <param name="TopicsPerReferral">Topics granted per qualified referral (for messaging).</param>
/// <param name="SpeakingCreditsPerReferral">Speaking credits granted per qualified referral.</param>
/// <param name="WritingCreditsPerReferral">Writing credits granted per qualified referral.</param>
public sealed record ReferralStatusDto(
    string Code,
    int InvitedCount,
    int QualifiedCount,
    int RewardCap,
    int BonusTopicsUnlocked,
    int RemainingSpeakingCredits,
    int RemainingWritingCredits,
    int TopicsPerReferral,
    int SpeakingCreditsPerReferral,
    int WritingCreditsPerReferral);
