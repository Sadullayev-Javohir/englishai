using Domain.Analytics;
using Domain.Learning;

namespace Application.Analytics.Dtos;

public sealed record DailyStudyBucketDto(DateOnly Day, int Seconds)
{
    public static DailyStudyBucketDto FromDomain(DailyStudyBucket b) => new(b.Day, b.Seconds);
}

public sealed record MonthlyStudyBucketDto(int Year, int Month, int Seconds)
{
    public static MonthlyStudyBucketDto FromDomain(MonthlyStudyBucket b) => new(b.Year, b.Month, b.Seconds);
}

public sealed record SkillStudyBucketDto(SkillType Skill, int Seconds)
{
    public static SkillStudyBucketDto FromDomain(SkillStudyBucket b) => new(b.Skill, b.Seconds);
}

/// <summary>
/// Study-time statistics for the progress dashboard: today/week/month/year/all-time totals
/// (in seconds) plus the chart series (7-day bars, 12-month bars, skill split, year heatmap).
/// </summary>
public sealed record StudyStatsDto(
    int TodaySeconds,
    int WeekSeconds,
    int MonthSeconds,
    int YearSeconds,
    int TotalSeconds,
    int ActiveDays,
    int CurrentDayStreak,
    int AverageSecondsPerActiveDay,
    int LongestDaySeconds,
    IReadOnlyList<DailyStudyBucketDto> Last7Days,
    IReadOnlyList<MonthlyStudyBucketDto> Last12Months,
    IReadOnlyList<SkillStudyBucketDto> BySkill,
    IReadOnlyList<DailyStudyBucketDto> YearHeatmap)
{
    public static StudyStatsDto FromDomain(StudyStats s) =>
        new(
            s.TodaySeconds,
            s.WeekSeconds,
            s.MonthSeconds,
            s.YearSeconds,
            s.TotalSeconds,
            s.ActiveDays,
            s.CurrentDayStreak,
            s.AverageSecondsPerActiveDay,
            s.LongestDaySeconds,
            s.Last7Days.Select(DailyStudyBucketDto.FromDomain).ToList(),
            s.Last12Months.Select(MonthlyStudyBucketDto.FromDomain).ToList(),
            s.BySkill.Select(SkillStudyBucketDto.FromDomain).ToList(),
            s.YearHeatmap.Select(DailyStudyBucketDto.FromDomain).ToList());
}
