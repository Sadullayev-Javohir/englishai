namespace Domain.Gamification;

/// <summary>
/// Pure streak logic (PROJECT-SPEC Faza 5). Given the set of days on which a learner met
/// their daily goal, it computes the current and longest consecutive runs as of a given
/// "today". Kept free of storage concerns so it is fully unit-testable; the actual set of
/// completed days is persisted via a port (Redis Sorted Set in production).
/// </summary>
public static class StreakCalculator
{
    public static StreakSnapshot Compute(IEnumerable<DateOnly> completedDays, DateOnly today)
    {
        var days = new HashSet<DateOnly>(completedDays);
        if (days.Count == 0)
            return StreakSnapshot.Empty;

        var isCompletedToday = days.Contains(today);

        // A live streak ends at today (if done) or yesterday (still extendable today).
        var anchor = isCompletedToday
            ? today
            : (days.Contains(today.AddDays(-1)) ? today.AddDays(-1) : (DateOnly?)null);

        var currentStreak = 0;
        if (anchor is { } end)
        {
            var cursor = end;
            while (days.Contains(cursor))
            {
                currentStreak++;
                cursor = cursor.AddDays(-1);
            }
        }

        var longestStreak = ComputeLongest(days);
        var isAtRisk = currentStreak > 0 && !isCompletedToday;

        return new StreakSnapshot(currentStreak, longestStreak, isCompletedToday, isAtRisk);
    }

    private static int ComputeLongest(IReadOnlyCollection<DateOnly> days)
    {
        var ordered = days.OrderBy(d => d).ToList();
        var longest = 1;
        var run = 1;

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i] == ordered[i - 1].AddDays(1))
                run++;
            else
                run = 1;

            if (run > longest)
                longest = run;
        }

        return longest;
    }
}
