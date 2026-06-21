using Domain.Common;

namespace Domain.Subscription;

/// <summary>
/// Metered features subject to free-tier limits (PROJECT-SPEC H.1).
///
/// The numeric value is part of the Redis usage key, so members may only ever be APPENDED -
/// renumbering an existing one silently hands every learner a fresh allowance and reads another
/// feature's counter.
/// </summary>
public enum PremiumFeature
{
    /// <summary>
    /// Starting an AI speaking session. The real cost guard is the daily minute budget
    /// (<see cref="SpeakingMinuteAllowance"/>); this only stops a learner from opening an unbounded
    /// number of rooms, so it is set high enough that the minute budget can actually be spent.
    /// </summary>
    SpeakingSession = 0,

    /// <summary>Adding a new SRS vocabulary word - Free: 10/day (reviews stay unlimited).</summary>
    NewVocabularyWord = 1,

    /// <summary>An AI writing assessment - Free: 1/day.</summary>
    WritingAssessment = 2,

    /// <summary>
    /// A question to the learning assistant - Free: 10/day. The most expensive single AI call in the
    /// app (a draft plus a verification pass), and until now the only one with no limit at all.
    /// </summary>
    AssistantQuestion = 3,

    /// <summary>
    /// A dedicated pronunciation exercise - Free: 3/day. Scored by Azure Pronunciation Assessment,
    /// which is billed per audio hour on top of speech-to-text (docs/development-guide.md rule 10).
    /// </summary>
    PronunciationDrill = 4,
}

/// <summary>The window over which a feature's usage is counted.</summary>
public enum UsagePeriod
{
    Daily = 0,
    Monthly = 1,
}

/// <summary>
/// The free-tier allowance for a metered feature (PROJECT-SPEC H.1). Premium has no limit.
/// Video/Listening is intentionally absent - it is unlimited on every tier (cheap, YouTube
/// embed).
/// </summary>
public sealed record FreeTierLimit(int MaxUses, UsagePeriod Period);

/// <summary>Outcome of a gate check for a feature.</summary>
/// <param name="IsAllowed">Whether the action may proceed.</param>
/// <param name="Limit">The applicable limit, or <see cref="Unlimited"/> for Premium.</param>
/// <param name="Used">Uses already consumed in the current period.</param>
/// <param name="Period">The reset window for the limit.</param>
public sealed record GateDecision(bool IsAllowed, int Limit, int Used, UsagePeriod Period)
{
    public const int Unlimited = -1;

    public int? Remaining => Limit == Unlimited ? null : Math.Max(0, Limit - Used);
}

/// <summary>
/// The freemium gating policy (PROJECT-SPEC H.1). Pure logic: given the tier and how much
/// of a feature has been used in the current period, it decides whether one more use is
/// allowed. Premium is always unlimited.
/// </summary>
public static class EntitlementPolicy
{
    private static readonly IReadOnlyDictionary<PremiumFeature, FreeTierLimit> FreeLimits =
        new Dictionary<PremiumFeature, FreeTierLimit>
        {
            // Raised from one: with a daily minute budget doing the real cost control, a single
            // session per day would trap the whole allowance in one sitting the learner may not
            // have time to finish.
            [PremiumFeature.SpeakingSession] = new(MaxUses: 5, UsagePeriod.Daily),
            [PremiumFeature.NewVocabularyWord] = new(MaxUses: 10, UsagePeriod.Daily),
            // Daily rather than monthly: three per month reads as "not included", while one a day is
            // a habit a free learner can actually build, at a comparable cost.
            [PremiumFeature.WritingAssessment] = new(MaxUses: 1, UsagePeriod.Daily),
            [PremiumFeature.AssistantQuestion] = new(MaxUses: 10, UsagePeriod.Daily),
            [PremiumFeature.PronunciationDrill] = new(MaxUses: 3, UsagePeriod.Daily),
        };

    public static FreeTierLimit FreeLimitFor(PremiumFeature feature) =>
        FreeLimits.TryGetValue(feature, out var limit)
            ? limit
            : throw new DomainException($"No free-tier limit defined for feature '{feature}'.");

    public static GateDecision Evaluate(PremiumFeature feature, bool isPremium, int usedInPeriod)
    {
        var freeLimit = FreeLimitFor(feature);

        if (isPremium)
            return new GateDecision(IsAllowed: true, GateDecision.Unlimited, usedInPeriod, freeLimit.Period);

        var allowed = usedInPeriod < freeLimit.MaxUses;
        return new GateDecision(allowed, freeLimit.MaxUses, usedInPeriod, freeLimit.Period);
    }
}
