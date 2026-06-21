namespace Domain.Retention;

/// <summary>
/// Pure D1/D7/D30 cohort-retention math (PROJECT-SPEC I.4). For each horizon N, a learner
/// counts toward the denominator only once N days have elapsed since they registered (so a
/// brand-new learner does not depress D30), and toward the numerator when they were active
/// on the calendar day exactly N days after registering. Free of storage and clock
/// concerns; the Application layer supplies <paramref name="asOf"/> via TimeProvider.
/// </summary>
public static class RetentionMetricsCalculator
{
    public const int Day1 = 1;
    public const int Day7 = 7;
    public const int Day30 = 30;

    public static RetentionMetrics Compute(IReadOnlyCollection<LearnerActivityWindow> windows, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(windows);

        return new RetentionMetrics(
            CohortSize: windows.Count,
            D1: RateFor(windows, asOf, Day1),
            D7: RateFor(windows, asOf, Day7),
            D30: RateFor(windows, asOf, Day30));
    }

    private static RetentionRate RateFor(IReadOnlyCollection<LearnerActivityWindow> windows, DateOnly asOf, int day)
    {
        var eligible = 0;
        var retained = 0;

        foreach (var window in windows)
        {
            var target = window.RegisteredOn.AddDays(day);
            if (target > asOf)
                continue; // Not enough time has passed to judge this learner at day N.

            eligible++;
            if (window.ActiveDays.Contains(target))
                retained++;
        }

        return new RetentionRate(day, eligible, retained);
    }
}
