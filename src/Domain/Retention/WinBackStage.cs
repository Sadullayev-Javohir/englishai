namespace Domain.Retention;

/// <summary>
/// Pure mapping from days-inactive to the win-back stage and its notification code
/// (PROJECT-SPEC I.2). The daily job calls <see cref="ForDaysInactive"/> and only dispatches
/// when the learner has crossed into a new stage (dedup tracked on the profile), so a
/// returning-but-still-quiet learner is never spammed daily. Uzbek text is template-only
/// (docs/development-guide.md rule 11) - this returns codes, never wording.
/// </summary>
public static class WinBackStage
{
    /// <summary>Win-back notification template codes, one per stage (docs/development-guide.md rule 11).</summary>
    public const string Day3Code = "notify.winback_day3";
    public const string Day7Code = "notify.winback_day7";
    public const string Day14Code = "notify.winback_day14";
    public const string Day30Code = "notify.winback_day30";
    public const string Day60Code = "notify.winback_day60";

    /// <summary>The stage a learner falls into given how many days they have been away.</summary>
    public static InactivityStage ForDaysInactive(int daysInactive)
    {
        if (daysInactive < 0)
            throw new ArgumentOutOfRangeException(nameof(daysInactive), "Days inactive cannot be negative.");

        return daysInactive switch
        {
            >= (int)InactivityStage.Day60Plus => InactivityStage.Day60Plus,
            >= (int)InactivityStage.Day30 => InactivityStage.Day30,
            >= (int)InactivityStage.Day14 => InactivityStage.Day14,
            >= (int)InactivityStage.Day7 => InactivityStage.Day7,
            >= (int)InactivityStage.Day3 => InactivityStage.Day3,
            _ => InactivityStage.Active
        };
    }

    /// <summary>The notification template code for a stage, or <c>null</c> for <see cref="InactivityStage.Active"/>.</summary>
    public static string? CodeFor(InactivityStage stage) => stage switch
    {
        InactivityStage.Day3 => Day3Code,
        InactivityStage.Day7 => Day7Code,
        InactivityStage.Day14 => Day14Code,
        InactivityStage.Day30 => Day30Code,
        InactivityStage.Day60Plus => Day60Code,
        _ => null
    };
}
