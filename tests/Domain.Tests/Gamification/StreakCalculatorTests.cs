using Domain.Gamification;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Gamification;

/// <summary>
/// Unit tests for streak logic (PROJECT-SPEC Faza 5). "Today" is always passed in
/// explicitly - never DateTime.Now (docs/development-guide.md 17.2).
/// </summary>
public class StreakCalculatorTests
{
    private static readonly DateOnly Today = new(2026, 6, 22);

    [Fact]
    public void No_completed_days_is_an_empty_streak()
    {
        var snapshot = StreakCalculator.Compute(Array.Empty<DateOnly>(), Today);

        snapshot.Should().Be(StreakSnapshot.Empty);
    }

    [Fact]
    public void Consecutive_days_ending_today_count_and_today_is_done()
    {
        var days = new[] { Today.AddDays(-2), Today.AddDays(-1), Today };

        var snapshot = StreakCalculator.Compute(days, Today);

        snapshot.CurrentStreak.Should().Be(3);
        snapshot.LongestStreak.Should().Be(3);
        snapshot.IsCompletedToday.Should().BeTrue();
        snapshot.IsAtRisk.Should().BeFalse();
    }

    [Fact]
    public void Streak_ending_yesterday_is_still_alive_but_at_risk()
    {
        var days = new[] { Today.AddDays(-2), Today.AddDays(-1) };

        var snapshot = StreakCalculator.Compute(days, Today);

        snapshot.CurrentStreak.Should().Be(2);
        snapshot.IsCompletedToday.Should().BeFalse();
        snapshot.IsAtRisk.Should().BeTrue();
    }

    [Fact]
    public void A_gap_before_yesterday_breaks_the_current_streak()
    {
        // Completed three days ago and four days ago, but not yesterday or today.
        var days = new[] { Today.AddDays(-4), Today.AddDays(-3) };

        var snapshot = StreakCalculator.Compute(days, Today);

        snapshot.CurrentStreak.Should().Be(0);
        snapshot.LongestStreak.Should().Be(2);
        snapshot.IsAtRisk.Should().BeFalse();
    }

    [Fact]
    public void Longest_streak_reflects_the_best_past_run_even_when_current_is_shorter()
    {
        var days = new[]
        {
            // A 4-day run a while ago.
            Today.AddDays(-10), Today.AddDays(-9), Today.AddDays(-8), Today.AddDays(-7),
            // A current 1-day run (today only).
            Today,
        };

        var snapshot = StreakCalculator.Compute(days, Today);

        snapshot.CurrentStreak.Should().Be(1);
        snapshot.LongestStreak.Should().Be(4);
    }

    [Fact]
    public void Unordered_duplicate_days_are_handled()
    {
        var days = new[] { Today, Today.AddDays(-1), Today, Today.AddDays(-1) };

        var snapshot = StreakCalculator.Compute(days, Today);

        snapshot.CurrentStreak.Should().Be(2);
        snapshot.LongestStreak.Should().Be(2);
    }
}
