using Application.Common;
using Domain.Subscription;

namespace Application.Subscription;

/// <summary>
/// Builds the key that identifies a quota window. Shared, because two counters that disagree on when
/// "today" starts produce quotas that reset at different times for the same learner - which is what
/// happened while the entitlement counter used the learner's local day (UTC+5) and the Voice Live
/// minute counter used the UTC day.
///
/// Windows follow Uzbekistan local time (<see cref="AppClock.UzbekistanOffset"/>), so a daily
/// allowance renews at local midnight rather than at 05:00 local.
/// </summary>
public static class UsagePeriodKey
{
    public static string Daily(DateTimeOffset now) =>
        $"D:{now.ToOffset(AppClock.UzbekistanOffset):yyyy-MM-dd}";

    public static string Monthly(DateTimeOffset now) =>
        $"M:{now.ToOffset(AppClock.UzbekistanOffset):yyyy-MM}";

    public static string For(UsagePeriod period, DateTimeOffset now) => period switch
    {
        UsagePeriod.Monthly => Monthly(now),
        _ => Daily(now),
    };

    public static string For(PremiumFeature feature, DateTimeOffset now) =>
        For(EntitlementPolicy.FreeLimitFor(feature).Period, now);
}
