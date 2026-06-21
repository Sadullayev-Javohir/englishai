using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Vocabulary;

public class VocabularyStatsCalculatorTests
{
    private static readonly Guid Learner = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Compute_summarizes_stages_due_growth_and_failures()
    {
        var dueFailed = VocabularyItem.Learn(Learner, "alpha", "a", Now.AddDays(-10));
        dueFailed.RecordReview(false, Now.AddDays(-4));
        var recent = VocabularyItem.Learn(Learner, "beta", "b", Now.AddDays(-2));
        var mastered = VocabularyItem.Learn(Learner, "gamma", "g", Now.AddDays(-40));
        mastered.RecordReview(true, Now.AddDays(-37));
        mastered.RecordReview(true, Now.AddDays(-33));
        mastered.RecordReview(true, Now.AddDays(-10));

        var result = VocabularyStatsCalculator.Compute([dueFailed, recent, mastered], Now);

        result.Total.Should().Be(3);
        result.Learning.Should().Be(2);
        result.Mastered.Should().Be(1);
        result.Due.Should().Be(1);
        result.AddedLast7Days.Should().Be(1);
        result.AddedLast30Days.Should().Be(2);
        result.TotalFailCount.Should().Be(1);
        result.StageBreakdown.Should().ContainKey(ReviewStage.Day3).WhoseValue.Should().Be(2);
        result.StageBreakdown.Should().ContainKey(ReviewStage.Mastered).WhoseValue.Should().Be(1);
    }

    [Fact]
    public void Compute_empty_items_returns_all_zero_with_every_stage()
    {
        var result = VocabularyStatsCalculator.Compute([], Now);

        result.Total.Should().Be(0);
        result.StageBreakdown.Keys.Should().BeEquivalentTo(Enum.GetValues<ReviewStage>());
        result.StageBreakdown.Values.Should().OnlyContain(count => count == 0);
    }
}
