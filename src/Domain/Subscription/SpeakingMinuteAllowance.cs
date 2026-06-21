namespace Domain.Subscription;

/// <summary>
/// The daily budget of learner speech an account may send to speech-to-text.
///
/// Speaking is the only feature whose cost scales with how long a learner talks rather than with how
/// many times they press a button, so it is the one quota that has to be measured in minutes. A
/// session count cannot express it: one learner's session is thirty seconds and another's is nine
/// minutes, and it is the second learner who costs money.
///
/// The Free budget is sized to be genuinely usable every day - enough for a real short conversation,
/// not a demo - while staying inside what a free account can be given away. Premium is sized well
/// above what an engaged learner actually uses, so the cap only ever catches the extreme tail.
/// </summary>
public static class SpeakingMinuteAllowance
{
    public const double FreeDailyMinutes = 5;
    public const double PremiumDailyMinutes = 20;

    public static double DailyLimit(bool isPremium) =>
        isPremium ? PremiumDailyMinutes : FreeDailyMinutes;

    /// <summary>
    /// Whether another utterance may be sent. The check is deliberately "has any budget left"
    /// rather than "has room for the whole utterance": the learner has already spoken by the time
    /// this runs, and refusing a turn that would tip them one second over the limit would throw away
    /// speech they cannot get back. The small overshoot is bounded by one utterance.
    /// </summary>
    public static SpeakingMinuteDecision Evaluate(bool isPremium, double usedMinutes)
    {
        var limit = DailyLimit(isPremium);
        var used = Math.Max(0, usedMinutes);
        return new SpeakingMinuteDecision(used < limit, limit, used, Math.Max(0, limit - used));
    }
}

/// <param name="IsAllowed">Whether another utterance may be sent.</param>
/// <param name="LimitMinutes">The account's daily budget.</param>
/// <param name="UsedMinutes">Minutes of speech already sent today.</param>
/// <param name="RemainingMinutes">What is left, never negative.</param>
public sealed record SpeakingMinuteDecision(
    bool IsAllowed,
    double LimitMinutes,
    double UsedMinutes,
    double RemainingMinutes);
