using Application.Analytics.GetStudyStats;
using Application.Analytics.RecordStudyTime;
using Application.Analytics.Ports;
using Application.Tests.Learning;
using Domain.Analytics;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Analytics;

public class AnalyticsHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 25);

    private readonly IStudyLogStore _store = Substitute.For<IStudyLogStore>();

    [Fact]
    public async Task RecordStudyTime_credits_the_store_for_the_clients_local_day()
    {
        var handler = new RecordStudyTimeCommandHandler(_store);

        await handler.Handle(
            new RecordStudyTimeCommand(Learner, SkillType.Speaking, 30, Today), CancellationToken.None);

        await _store.Received(1).AddStudyTimeAsync(
            Learner, Today, SkillType.Speaking, 30, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStudyStats_aggregates_the_learners_records()
    {
        var record = DailyStudyRecord.Start(Learner, Today);
        record.AddTime(SkillType.Reading, 90);
        _store.GetStatsAsync(Learner, Today, Arg.Any<CancellationToken>())
            .Returns(StudyStatsCalculator.Compute([record], Today));

        var handler = new GetStudyStatsQueryHandler(_store);

        var stats = await handler.Handle(new GetStudyStatsQuery(Learner, Today), CancellationToken.None);

        stats.TodaySeconds.Should().Be(90);
        stats.TotalSeconds.Should().Be(90);
        stats.BySkill.Single(b => b.Skill == SkillType.Reading).Seconds.Should().Be(90);
        stats.Last7Days.Should().HaveCount(7);
        stats.YearHeatmap.Should().HaveCount(365);
    }

    [Theory]
    [InlineData(0, true)]      // non-positive seconds → invalid
    [InlineData(30, false)]    // valid heartbeat
    [InlineData(10_000, true)] // above the cap → invalid
    public void RecordStudyTime_validator_bounds_seconds(int seconds, bool expectError)
    {
        var validator = new RecordStudyTimeCommandValidator(new FixedTimeProvider(Now));

        var result = validator.Validate(
            new RecordStudyTimeCommand(Learner, SkillType.Speaking, seconds, Today));

        result.IsValid.Should().Be(!expectError);
    }

    [Fact]
    public void RecordStudyTime_validator_rejects_a_far_off_date()
    {
        var validator = new RecordStudyTimeCommandValidator(new FixedTimeProvider(Now));

        var result = validator.Validate(
            new RecordStudyTimeCommand(Learner, SkillType.Speaking, 30, Today.AddDays(30)));

        result.IsValid.Should().BeFalse();
    }
}
