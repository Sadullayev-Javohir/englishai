using Domain.Learning;

namespace Domain.Analytics;

/// <summary>One day's total study time (used for the 7-day bars and the year heatmap).</summary>
public sealed record DailyStudyBucket(DateOnly Day, int Seconds);

/// <summary>One calendar month's total study time (used for the 12-month bars).</summary>
public sealed record MonthlyStudyBucket(int Year, int Month, int Seconds);

/// <summary>All-time study time spent on one skill (used for the skill distribution).</summary>
public sealed record SkillStudyBucket(SkillType Skill, int Seconds);

/// <summary>
/// The fully-computed study-time picture for the progress dashboard: headline totals for
/// today/this week/this month/this year/all-time, plus the series that power the charts.
/// Produced by <see cref="StudyStatsCalculator"/> from a learner's <see cref="DailyStudyRecord"/>
/// rows relative to their local "today".
/// </summary>
public sealed record StudyStats(
    int TodaySeconds,
    int WeekSeconds,
    int MonthSeconds,
    int YearSeconds,
    int TotalSeconds,
    int ActiveDays,
    int CurrentDayStreak,
    int AverageSecondsPerActiveDay,
    int LongestDaySeconds,
    IReadOnlyList<DailyStudyBucket> Last7Days,
    IReadOnlyList<MonthlyStudyBucket> Last12Months,
    IReadOnlyList<SkillStudyBucket> BySkill,
    IReadOnlyList<DailyStudyBucket> YearHeatmap)
{
    public static StudyStats Empty(DateOnly today) =>
        StudyStatsCalculator.Compute(Array.Empty<DailyStudyRecord>(), today);
}
