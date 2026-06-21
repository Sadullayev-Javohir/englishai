using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Learning;

public class SkillScoreCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    private static SkillActivity Activity(int score, double daysAgo) =>
        new(SkillType.Speaking, score, Now.AddDays(-daysAgo));

    [Fact]
    public void No_activity_falls_back_to_the_seed()
    {
        var result = SkillScoreCalculator.Compute(
            SkillType.Speaking, Array.Empty<SkillActivity>(), seedScore: 42, Now);

        result.Score.Should().Be(42);
        result.SampleCount.Should().Be(0);
    }

    [Fact]
    public void Single_recent_activity_returns_that_score()
    {
        var result = SkillScoreCalculator.Compute(
            SkillType.Speaking, new[] { Activity(80, daysAgo: 0) }, seedScore: 20, Now);

        result.Score.Should().Be(80);
        result.SampleCount.Should().Be(1);
    }

    [Fact]
    public void Recent_activity_is_weighted_more_than_old_activity()
    {
        // 90 today, 50 fourteen days ago (one half-life): the result must sit closer to 90.
        var activities = new[] { Activity(90, daysAgo: 0), Activity(50, daysAgo: 14) };

        var result = SkillScoreCalculator.Compute(SkillType.Speaking, activities, seedScore: 0, Now);

        // Weighted: (90*1 + 50*0.5) / 1.5 = 76.67.
        result.Score.Should().BeApproximately(76.7, 0.1);
        result.Score.Should().BeGreaterThan(70);
    }

    [Fact]
    public void Activity_older_than_the_window_is_ignored()
    {
        var activities = new[] { Activity(95, daysAgo: 45) };

        var result = SkillScoreCalculator.Compute(SkillType.Speaking, activities, seedScore: 30, Now);

        result.Score.Should().Be(30);
        result.SampleCount.Should().Be(0);
    }

    [Fact]
    public void Only_matching_skill_activities_count()
    {
        var activities = new[]
        {
            new SkillActivity(SkillType.Reading, 10, Now),
            new SkillActivity(SkillType.Speaking, 70, Now)
        };

        var result = SkillScoreCalculator.Compute(SkillType.Speaking, activities, seedScore: 0, Now);

        result.Score.Should().Be(70);
        result.SampleCount.Should().Be(1);
    }
}
