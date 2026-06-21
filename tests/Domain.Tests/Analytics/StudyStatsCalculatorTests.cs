using Domain.Analytics;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Analytics;

public class StudyStatsCalculatorTests
{
    // A Thursday - its Monday-based week starts on 2026-06-22.
    private static readonly DateOnly Today = new(2026, 6, 25);

    private static DailyStudyRecord Record(DateOnly day, int seconds, SkillType skill = SkillType.Speaking)
    {
        var record = DailyStudyRecord.Start(Guid.NewGuid(), day);
        record.AddTime(skill, Math.Min(seconds, DailyStudyRecord.MaxHeartbeatSeconds));
        // Build up larger totals with repeated capped heartbeats.
        var remaining = seconds - DailyStudyRecord.MaxHeartbeatSeconds;
        while (remaining > 0)
        {
            record.AddTime(skill, Math.Min(remaining, DailyStudyRecord.MaxHeartbeatSeconds));
            remaining -= DailyStudyRecord.MaxHeartbeatSeconds;
        }
        return record;
    }

    [Fact]
    public void Empty_history_yields_zero_totals_and_full_series()
    {
        var stats = StudyStatsCalculator.Compute(Array.Empty<DailyStudyRecord>(), Today);

        stats.TodaySeconds.Should().Be(0);
        stats.WeekSeconds.Should().Be(0);
        stats.MonthSeconds.Should().Be(0);
        stats.YearSeconds.Should().Be(0);
        stats.TotalSeconds.Should().Be(0);
        stats.ActiveDays.Should().Be(0);
        stats.AverageSecondsPerActiveDay.Should().Be(0);

        stats.Last7Days.Should().HaveCount(7);
        stats.Last12Months.Should().HaveCount(12);
        stats.YearHeatmap.Should().HaveCount(365);
        stats.BySkill.Should().HaveCount(6);
        stats.Last7Days.Last().Day.Should().Be(Today);
        stats.YearHeatmap.Last().Day.Should().Be(Today);
    }

    [Fact]
    public void Totals_respect_today_week_month_year_boundaries()
    {
        var records = new[]
        {
            Record(Today, 60),                       // today, this week, month, year
            Record(Today.AddDays(-1), 100),          // Wed - this week, month, year
            Record(new DateOnly(2026, 6, 22), 50),   // Monday (week start) - this week, month, year
            Record(new DateOnly(2026, 6, 21), 40),   // Sunday before - month + year, NOT this week
            Record(new DateOnly(2026, 5, 30), 30),   // May - year only
            Record(new DateOnly(2025, 12, 31), 20),  // last year - total only
        };

        var stats = StudyStatsCalculator.Compute(records, Today);

        stats.TodaySeconds.Should().Be(60);
        stats.WeekSeconds.Should().Be(60 + 100 + 50);
        stats.MonthSeconds.Should().Be(60 + 100 + 50 + 40);       // June only (the 30 is May)
        stats.YearSeconds.Should().Be(60 + 100 + 50 + 40 + 30);   // 2026, includes May
        stats.TotalSeconds.Should().Be(60 + 100 + 50 + 40 + 30 + 20);
    }

    [Fact]
    public void Active_days_average_and_longest_are_computed()
    {
        var records = new[]
        {
            Record(Today, 120),
            Record(Today.AddDays(-2), 60),
            Record(Today.AddDays(-3), 240),
        };

        var stats = StudyStatsCalculator.Compute(records, Today);

        stats.ActiveDays.Should().Be(3);
        stats.LongestDaySeconds.Should().Be(240);
        stats.AverageSecondsPerActiveDay.Should().Be((120 + 60 + 240) / 3);
    }

    [Fact]
    public void By_skill_splits_all_time_time_per_skill()
    {
        var records = new[]
        {
            Record(Today, 60, SkillType.Speaking),
            Record(Today.AddDays(-1), 100, SkillType.Reading),
            Record(Today.AddDays(-2), 40, SkillType.Speaking),
        };

        var stats = StudyStatsCalculator.Compute(records, Today);

        stats.BySkill.Single(b => b.Skill == SkillType.Speaking).Seconds.Should().Be(100);
        stats.BySkill.Single(b => b.Skill == SkillType.Reading).Seconds.Should().Be(100);
        stats.BySkill.Single(b => b.Skill == SkillType.Writing).Seconds.Should().Be(0);
    }

    [Fact]
    public void Current_day_streak_counts_consecutive_studied_days_ending_today()
    {
        var records = new[]
        {
            Record(Today, 60),
            Record(Today.AddDays(-1), 60),
            Record(Today.AddDays(-2), 60),
            // gap on day -3
            Record(Today.AddDays(-4), 60),
        };

        var stats = StudyStatsCalculator.Compute(records, Today);

        stats.CurrentDayStreak.Should().Be(3);
    }

    [Fact]
    public void Current_day_streak_stays_alive_when_today_not_yet_studied()
    {
        var records = new[]
        {
            Record(Today.AddDays(-1), 60),
            Record(Today.AddDays(-2), 60),
        };

        var stats = StudyStatsCalculator.Compute(records, Today);

        stats.CurrentDayStreak.Should().Be(2);
    }

    [Fact]
    public void Last7Days_is_zero_filled_and_ordered_oldest_first()
    {
        var records = new[] { Record(Today, 60), Record(Today.AddDays(-3), 90) };

        var stats = StudyStatsCalculator.Compute(records, Today);

        stats.Last7Days.Should().HaveCount(7);
        stats.Last7Days[0].Day.Should().Be(Today.AddDays(-6));
        stats.Last7Days[3].Seconds.Should().Be(90);
        stats.Last7Days[6].Seconds.Should().Be(60);
        stats.Last7Days[1].Seconds.Should().Be(0);
    }

    [Fact]
    public void Last12Months_buckets_by_calendar_month()
    {
        var records = new[]
        {
            Record(Today, 60),                      // June 2026
            Record(new DateOnly(2026, 6, 1), 40),   // June 2026 (same bucket)
            Record(new DateOnly(2026, 5, 15), 30),  // May 2026
        };

        var stats = StudyStatsCalculator.Compute(records, Today);

        stats.Last12Months.Should().HaveCount(12);
        var june = stats.Last12Months.Single(m => m is { Year: 2026, Month: 6 });
        june.Seconds.Should().Be(100);
        stats.Last12Months.Single(m => m is { Year: 2026, Month: 5 }).Seconds.Should().Be(30);
        stats.Last12Months.Last().Should().Match<MonthlyStudyBucket>(m => m.Year == 2026 && m.Month == 6);
    }
}
