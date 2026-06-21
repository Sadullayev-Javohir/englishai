using Domain.Analytics;
using Domain.Common;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Analytics;

public class DailyStudyRecordTests
{
    private static readonly DateOnly Day = new(2026, 6, 25);

    [Fact]
    public void Start_initializes_an_empty_record()
    {
        var record = DailyStudyRecord.Start(Guid.NewGuid(), Day);

        record.TotalSeconds.Should().Be(0);
        record.Day.Should().Be(Day);
    }

    [Fact]
    public void Start_rejects_empty_learner()
    {
        var act = () => DailyStudyRecord.Start(Guid.Empty, Day);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddTime_accumulates_into_the_right_skill_and_total()
    {
        var record = DailyStudyRecord.Start(Guid.NewGuid(), Day);

        record.AddTime(SkillType.Speaking, 30);
        record.AddTime(SkillType.Speaking, 20);
        record.AddTime(SkillType.Reading, 15);

        record.SecondsFor(SkillType.Speaking).Should().Be(50);
        record.SecondsFor(SkillType.Reading).Should().Be(15);
        record.TotalSeconds.Should().Be(65);
    }

    [Fact]
    public void AddTime_clamps_a_single_heartbeat_to_the_cap()
    {
        var record = DailyStudyRecord.Start(Guid.NewGuid(), Day);

        record.AddTime(SkillType.Grammar, DailyStudyRecord.MaxHeartbeatSeconds + 10_000);

        record.SecondsFor(SkillType.Grammar).Should().Be(DailyStudyRecord.MaxHeartbeatSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddTime_rejects_non_positive_seconds(int seconds)
    {
        var record = DailyStudyRecord.Start(Guid.NewGuid(), Day);

        var act = () => record.AddTime(SkillType.Writing, seconds);

        act.Should().Throw<DomainException>();
    }
}
