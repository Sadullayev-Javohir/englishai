namespace Application.Common;

/// <summary>
/// Resolves the learner's <em>local</em> calendar day for all daily boundaries (daily-goal plan,
/// streaks, per-day free-tier limits). The audience is in Uzbekistan, which is UTC+5 year-round
/// (no daylight saving), so a learner's "day" must roll over at local midnight (00:00 Tashkent),
/// not UTC midnight - which falls at 05:00 local and would otherwise keep the previous day's daily
/// plan and used-up free Speaking session locked for the first five hours of the new day.
///
/// Every "today" computation funnels through here so each daily boundary (gamification, entitlement
/// usage windows, …) agrees on when the day flips. A fixed offset is used rather than
/// <see cref="TimeZoneInfo"/> so behaviour never depends on the host having tz database entries.
/// </summary>
public static class AppClock
{
    /// <summary>Uzbekistan's fixed UTC offset (UTC+5, no daylight saving).</summary>
    public static readonly TimeSpan UzbekistanOffset = TimeSpan.FromHours(5);

    /// <summary>The current moment expressed in Uzbekistan local time.</summary>
    public static DateTimeOffset LocalNow(this TimeProvider clock) =>
        clock.GetUtcNow().ToOffset(UzbekistanOffset);

    /// <summary>Today's calendar date in Uzbekistan local time.</summary>
    public static DateOnly LocalToday(this TimeProvider clock) => clock.GetUtcNow().ToLocalDate();

    /// <summary>The Uzbekistan local calendar date for a given moment.</summary>
    public static DateOnly ToLocalDate(this DateTimeOffset moment) =>
        DateOnly.FromDateTime(moment.ToOffset(UzbekistanOffset).DateTime);
}
