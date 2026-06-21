using Domain.Common;
using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Vocabulary;

/// <summary>
/// Unit tests for the 3/7/21-day SRS ladder (PROJECT-SPEC B.1). Time is controlled
/// explicitly via the passed instant - never DateTime.Now (docs/development-guide.md 17.2).
/// </summary>
public class ReviewScheduleTests
{
    private static readonly DateTimeOffset LearnedAt = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_schedule_is_due_three_days_after_learning()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);

        schedule.Stage.Should().Be(ReviewStage.Day3);
        schedule.FailCount.Should().Be(0);
        schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(3));
        schedule.IsDue(LearnedAt.AddDays(3)).Should().BeTrue();
        schedule.IsDue(LearnedAt.AddDays(2)).Should().BeFalse();
    }

    [Fact]
    public void Passing_each_stage_climbs_3_then_7_then_21_then_mastered()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);

        schedule.RecordResult(passed: true, LearnedAt.AddDays(3));
        schedule.Stage.Should().Be(ReviewStage.Day7);
        schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(7));

        schedule.RecordResult(passed: true, LearnedAt.AddDays(7));
        schedule.Stage.Should().Be(ReviewStage.Day21);
        schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(21));

        schedule.RecordResult(passed: true, LearnedAt.AddDays(21));
        schedule.Stage.Should().Be(ReviewStage.Mastered);
        schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(111));
        schedule.IsDue(LearnedAt.AddDays(111)).Should().BeTrue();
    }

    [Fact]
    public void Intervals_are_measured_from_the_learning_day_not_the_review_day()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);

        // Learner reviews late, on day 5 - the day-7 review is still anchored to learning.
        schedule.RecordResult(passed: true, LearnedAt.AddDays(5));

        schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(7));
    }

    [Fact]
    public void Failing_restarts_the_cycle_from_now_and_increments_fail_count()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);
        schedule.RecordResult(passed: true, LearnedAt.AddDays(3)); // now at Day7

        var failedAt = LearnedAt.AddDays(7);
        schedule.RecordResult(passed: false, failedAt);

        schedule.Stage.Should().Be(ReviewStage.Day3);
        schedule.FailCount.Should().Be(1);
        schedule.LearnedAt.Should().Be(failedAt);
        schedule.NextReviewAt.Should().Be(failedAt.AddDays(3));
    }

    [Fact]
    public void Passing_a_mastered_maintenance_review_schedules_the_next_one_in_180_days()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);
        schedule.RecordResult(true, LearnedAt.AddDays(3));
        schedule.RecordResult(true, LearnedAt.AddDays(7));
        schedule.RecordResult(true, LearnedAt.AddDays(21));

        var maintenanceAt = LearnedAt.AddDays(111);
        schedule.RecordResult(true, maintenanceAt, responseLatencyMs: 2400, hintCount: 1);

        schedule.Stage.Should().Be(ReviewStage.Mastered);
        schedule.NextReviewAt.Should().Be(maintenanceAt.AddDays(180));
        schedule.LastResponseLatencyMs.Should().Be(2400);
        schedule.LastHintCount.Should().Be(1);
        schedule.ReviewCount.Should().Be(4);
    }

    [Fact]
    public void Review_records_latency_and_hint_telemetry()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);

        schedule.RecordResult(true, LearnedAt.AddDays(3), responseLatencyMs: 1800, hintCount: 2);

        schedule.LastResponseLatencyMs.Should().Be(1800);
        schedule.LastHintCount.Should().Be(2);
        schedule.ReviewCount.Should().Be(1);
    }

    [Fact]
    public void Late_second_review_does_not_delay_the_day21_checkpoint()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);
        schedule.RecordResult(true, LearnedAt.AddDays(3));
        schedule.RecordResult(true, LearnedAt.AddDays(25));

        schedule.Stage.Should().Be(ReviewStage.Day21);
        ((int)schedule.Stage).Should().Be(2);
        schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(21));
        schedule.IsDue(LearnedAt.AddDays(25)).Should().BeTrue();
    }

    [Fact]
    public void Failed_maintenance_review_preserves_seven_day_recovery()
    {
        var schedule = ReviewSchedule.StartNew(LearnedAt);
        schedule.RecordResult(true, LearnedAt.AddDays(3));
        schedule.RecordResult(true, LearnedAt.AddDays(7));
        schedule.RecordResult(true, LearnedAt.AddDays(21));
        var failedAt = LearnedAt.AddDays(111);
        schedule.RecordResult(false, failedAt);

        schedule.Stage.Should().Be(ReviewStage.Day21);
        schedule.NextReviewAt.Should().Be(failedAt.AddDays(7));
        schedule.FailCount.Should().Be(1);
    }
}
