using Application.Common;
using Application.Gamification;
using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Subscription.Access;
using Application.Tests.Learning;
using Domain.Assessment;
using Domain.Gamification;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Gamification;

/// <summary>
/// Unit tests for the leaderboard/points feature's award orchestration. Ports are mocked per
/// docs/development-guide.md §7 (Application handlers: mock external dependencies).
/// </summary>
public class PointsServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = Now.ToLocalDate();
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly ILearnerPointsRepository _points = Substitute.For<ILearnerPointsRepository>();
    private readonly ILeaderboardStore _leaderboard = Substitute.For<ILeaderboardStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IGamificationStore _gamification = Substitute.For<IGamificationStore>();

    private readonly PointsService _service;

    public PointsServiceTests()
    {
        _points.GetOrCreateAsync(Learner, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(_ => LearnerPoints.CreateNew(Learner, Now));
        _gamification.GetCompletedDaysAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DateOnly>());
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((LearnerProfile?)null);

        _service = new PointsService(_points, _leaderboard, _profiles, _gamification);
    }

    private static LearnerProfile ProfileAt(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    [Fact]
    public async Task Plain_module_completion_earns_the_base_award()
    {
        // Two skills already practiced today, a third just completed: no first-activity bonus,
        // no all-six bonus.
        await _service.AwardForActivityAsync(
            Learner, 80, new[] { SkillType.Speaking, SkillType.Listening }, Now, CancellationToken.None);

        await _points.Received(1).SaveAsync(
            Arg.Is<LearnerPoints>(p => p.LifetimeXp == PointsPolicy.ModuleCompletionPoints),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task First_activity_of_the_day_adds_the_daily_bonus()
    {
        await _service.AwardForActivityAsync(Learner, 80, Array.Empty<SkillType>(), Now, CancellationToken.None);

        var expected = PointsPolicy.ModuleCompletionPoints + PointsPolicy.DailyActivityBonusPoints;
        await _points.Received(1).SaveAsync(
            Arg.Is<LearnerPoints>(p => p.LifetimeXp == expected), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Completing_the_sixth_distinct_skill_today_adds_the_all_modules_bonus()
    {
        var fiveSkills = new[]
        {
            SkillType.Speaking, SkillType.Listening, SkillType.Reading, SkillType.Writing, SkillType.Grammar,
        };

        await _service.AwardForActivityAsync(Learner, 80, fiveSkills, Now, CancellationToken.None);

        var expected = PointsPolicy.ModuleCompletionPoints + PointsPolicy.AllModulesBonusPoints;
        await _points.Received(1).SaveAsync(
            Arg.Is<LearnerPoints>(p => p.LifetimeXp == expected), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Premium_subscribers_do_not_get_a_leaderboard_multiplier()
    {
        // XP is deliberately independent of subscription state: a paid multiplier would make the
        // leaderboard a ranking of who spent money rather than who practised.
        await _service.AwardForActivityAsync(
            Learner, 80, new[] { SkillType.Speaking, SkillType.Listening }, Now, CancellationToken.None);

        await _points.Received(1).SaveAsync(
            Arg.Is<LearnerPoints>(p => p.LifetimeXp == PointsPolicy.ModuleCompletionPoints),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(75, 10)]
    [InlineData(85, 15)]
    [InlineData(95, 20)]
    public async Task Activity_quality_controls_the_base_xp(int score, int expectedXp)
    {
        await _service.AwardForActivityAsync(
            Learner, score, new[] { SkillType.Speaking }, Now, CancellationToken.None);

        await _points.Received(1).SaveAsync(
            Arg.Is<LearnerPoints>(points => points.LifetimeXp == expectedXp),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Newly_reaching_a_streak_milestone_adds_its_bonus_once()
    {
        // Seven consecutive completed days ending today: crosses the 7-day milestone.
        var completedDays = Enumerable.Range(0, 7).Select(i => Today.AddDays(-i)).ToArray();
        _gamification.GetCompletedDaysAsync(Learner, Arg.Any<CancellationToken>()).Returns(completedDays);

        await _service.AwardForActivityAsync(
            Learner, 80, new[] { SkillType.Speaking, SkillType.Listening }, Now, CancellationToken.None);

        var expected = PointsPolicy.ModuleCompletionPoints + 50; // 7-day milestone bonus
        await _points.Received(1).SaveAsync(
            Arg.Is<LearnerPoints>(p => p.LifetimeXp == expected), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pushes_the_updated_score_onto_the_learners_current_level_leaderboard()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B2));

        await _service.AwardForActivityAsync(
            Learner, 80, new[] { SkillType.Speaking, SkillType.Listening }, Now, CancellationToken.None);

        await _leaderboard.Received(1).SetScoreAsync(
            Learner, CefrLevel.B2, PointsPolicy.ModuleCompletionPoints, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Defaults_to_A2_leaderboard_when_the_learner_has_no_profile_yet()
    {
        await _service.AwardForActivityAsync(
            Learner, 80, new[] { SkillType.Speaking, SkillType.Listening }, Now, CancellationToken.None);

        await _leaderboard.Received(1).SetScoreAsync(
            Learner, CefrLevel.A2, Arg.Any<long>(), Arg.Any<CancellationToken>());
    }
}
