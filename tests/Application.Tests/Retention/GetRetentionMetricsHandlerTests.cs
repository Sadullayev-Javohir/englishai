using Application.Learning.Ports;
using Application.Retention.GetRetentionMetrics;
using Application.Tests.Learning;
using Domain.Assessment;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Retention;

/// <summary>
/// Unit tests for the cohort retention-metrics query (PROJECT-SPEC I.4).
/// </summary>
public class GetRetentionMetricsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private GetRetentionMetricsQueryHandler NewHandler() => new(_profiles, _clock);

    private static LearnerProfile ProfileCreatedDaysAgo(int days, params int[] activeOffsetsFromRegistration)
    {
        var createdAt = Now.AddDays(-days);
        var profile = LearnerProfile.CreateFromPlacement(
            Guid.NewGuid(),
            new PlacementResult(CefrLevel.B1, CefrLevel.B1.ToScore(), new Dictionary<TestStage, StageResult>()),
            createdAt);
        foreach (var offset in activeOffsetsFromRegistration)
            profile.RecordActivity(SkillType.Reading, 80, createdAt.AddDays(offset));
        return profile;
    }

    [Fact]
    public async Task Computes_day1_retention_across_the_cohort()
    {
        // One learner returned on D1, one did not; both registered 10 days ago.
        var retained = ProfileCreatedDaysAgo(10, 1);
        var lapsed = ProfileCreatedDaysAgo(10);
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LearnerProfile> { retained, lapsed });

        var result = await NewHandler().Handle(new GetRetentionMetricsQuery(), CancellationToken.None);

        result.CohortSize.Should().Be(2);
        result.D1.EligibleLearners.Should().Be(2);
        result.D1.RetainedLearners.Should().Be(1);
        result.D1.RatePercent.Should().Be(50.0);
    }

    [Fact]
    public async Task A_brand_new_learner_is_not_counted_for_longer_horizons()
    {
        var profile = ProfileCreatedDaysAgo(2, 1); // eligible only for D1
        _profiles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LearnerProfile> { profile });

        var result = await NewHandler().Handle(new GetRetentionMetricsQuery(), CancellationToken.None);

        result.D1.EligibleLearners.Should().Be(1);
        result.D7.EligibleLearners.Should().Be(0);
        result.D30.EligibleLearners.Should().Be(0);
    }

    [Fact]
    public async Task An_empty_cohort_is_handled()
    {
        _profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<LearnerProfile>());

        var result = await NewHandler().Handle(new GetRetentionMetricsQuery(), CancellationToken.None);

        result.CohortSize.Should().Be(0);
        result.D1.RatePercent.Should().Be(0);
    }
}
