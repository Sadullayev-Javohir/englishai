namespace Application.Common;

/// <summary>
/// The learner has used their daily speaking budget. Surfaced as HTTP 402 with a machine code, the
/// same contract <see cref="FeatureLimitExceededException"/> uses: the frontend already routes 402
/// into the upgrade flow, and this is an upsell rather than throttling - a 429 would land in the
/// client's retry/backoff path and the learner would just see a spinner.
/// </summary>
public sealed class SpeakingMinutesExhaustedException : Exception
{
    public const string Code = "speaking_minutes_exhausted";

    public SpeakingMinutesExhaustedException(double limitMinutes, double usedMinutes, DateTimeOffset resetsAt)
        : base($"The daily speaking allowance of {limitMinutes:0.#} minutes is used up.")
    {
        LimitMinutes = limitMinutes;
        UsedMinutes = usedMinutes;
        ResetsAt = resetsAt;
    }

    public double LimitMinutes { get; }
    public double UsedMinutes { get; }

    /// <summary>When the allowance renews (the learner's next local midnight).</summary>
    public DateTimeOffset ResetsAt { get; }
}
