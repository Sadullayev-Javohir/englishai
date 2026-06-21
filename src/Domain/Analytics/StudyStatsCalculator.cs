using Domain.Learning;

namespace Domain.Analytics;

/// <summary>
/// Pure aggregation of a learner's daily study records into the progress-dashboard statistics
/// (PROJECT-SPEC Faza 2). Free of storage and clock concerns - it takes the records and the
/// learner's local "today" as inputs, so every boundary (today/week/month/year) is fully
/// unit-testable and matches the learner's own calendar rather than UTC.
/// </summary>
public static class StudyStatsCalculator
{
    private const int HeatmapDays = 365;
    private const int RecentDays = 7;
    private const int RecentMonths = 12;

    private static readonly SkillType[] AllSkills =
    {
        SkillType.Speaking, SkillType.Listening, SkillType.Reading,
        SkillType.Writing, SkillType.Grammar, SkillType.Vocabulary,
    };

    public static StudyStats Compute(
        IEnumerable<DailyStudyRecord> records,
        DateOnly today,
        DayOfWeek weekStart = DayOfWeek.Monday)
    {
        // Collapse to one entry per day (defensive: the store keeps one row per (learner, day),
        // but summing is harmless if duplicates ever slip in).
        var byDay = new Dictionary<DateOnly, int>();
        var bySkill = AllSkills.ToDictionary(s => s, _ => 0);
        var total = 0;
        var longestDay = 0;

        foreach (var record in records)
        {
            byDay[record.Day] = byDay.GetValueOrDefault(record.Day) + record.TotalSeconds;
            foreach (var skill in AllSkills)
                bySkill[skill] += record.SecondsFor(skill);
            total += record.TotalSeconds;
        }

        foreach (var seconds in byDay.Values)
            longestDay = Math.Max(longestDay, seconds);

        var weekStartDay = StartOfWeek(today, weekStart);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var yearStart = new DateOnly(today.Year, 1, 1);

        var todaySeconds = byDay.GetValueOrDefault(today);
        var weekSeconds = SumRange(byDay, weekStartDay, today);
        var monthSeconds = SumRange(byDay, monthStart, today);
        var yearSeconds = SumRange(byDay, yearStart, today);

        var activeDays = byDay.Count(kvp => kvp.Value > 0);
        var average = activeDays == 0 ? 0 : total / activeDays;

        return new StudyStats(
            TodaySeconds: todaySeconds,
            WeekSeconds: weekSeconds,
            MonthSeconds: monthSeconds,
            YearSeconds: yearSeconds,
            TotalSeconds: total,
            ActiveDays: activeDays,
            CurrentDayStreak: CurrentDayStreak(byDay, today),
            AverageSecondsPerActiveDay: average,
            LongestDaySeconds: longestDay,
            Last7Days: DailySeries(byDay, today, RecentDays),
            Last12Months: MonthlySeries(byDay, today, RecentMonths),
            BySkill: AllSkills.Select(s => new SkillStudyBucket(s, bySkill[s])).ToList(),
            YearHeatmap: DailySeries(byDay, today, HeatmapDays));
    }

    private static DateOnly StartOfWeek(DateOnly day, DayOfWeek weekStart)
    {
        var diff = ((int)day.DayOfWeek - (int)weekStart + 7) % 7;
        return day.AddDays(-diff);
    }

    private static int SumRange(IReadOnlyDictionary<DateOnly, int> byDay, DateOnly from, DateOnly to)
    {
        var sum = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
            sum += byDay.GetValueOrDefault(day);
        return sum;
    }

    // A run of consecutive days with study time, ending today (if studied) or yesterday (still
    // extendable today). Mirrors the gamification streak shape but keyed on "studied at all".
    private static int CurrentDayStreak(IReadOnlyDictionary<DateOnly, int> byDay, DateOnly today)
    {
        bool Active(DateOnly d) => byDay.GetValueOrDefault(d) > 0;

        DateOnly? anchor = Active(today)
            ? today
            : (Active(today.AddDays(-1)) ? today.AddDays(-1) : null);

        var streak = 0;
        if (anchor is { } end)
        {
            for (var cursor = end; Active(cursor); cursor = cursor.AddDays(-1))
                streak++;
        }

        return streak;
    }

    // The last `count` days ending on `today`, oldest first, with zero-filled gaps so charts
    // render a continuous timeline.
    private static List<DailyStudyBucket> DailySeries(
        IReadOnlyDictionary<DateOnly, int> byDay, DateOnly today, int count)
    {
        var series = new List<DailyStudyBucket>(count);
        for (var i = count - 1; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            series.Add(new DailyStudyBucket(day, byDay.GetValueOrDefault(day)));
        }
        return series;
    }

    // The last `count` calendar months ending on `today`'s month, oldest first, zero-filled.
    private static List<MonthlyStudyBucket> MonthlySeries(
        IReadOnlyDictionary<DateOnly, int> byDay, DateOnly today, int count)
    {
        var monthly = new Dictionary<(int Year, int Month), int>();
        foreach (var (day, seconds) in byDay)
            monthly[(day.Year, day.Month)] = monthly.GetValueOrDefault((day.Year, day.Month)) + seconds;

        var series = new List<MonthlyStudyBucket>(count);
        for (var i = count - 1; i >= 0; i--)
        {
            var anchor = new DateOnly(today.Year, today.Month, 1).AddMonths(-i);
            series.Add(new MonthlyStudyBucket(
                anchor.Year, anchor.Month, monthly.GetValueOrDefault((anchor.Year, anchor.Month))));
        }
        return series;
    }
}
