using Application.Common;
using Application.Gamification.Ports;
using Application.Learning.GetGrowth;
using Application.Learning.GetLearnerOverview;
using Application.Learning.GetRecommendations;
using Application.Learning.Ports;
using Application.Learning.RecordConfirmationTest;
using Application.Learning.RecordSkillActivity;
using Domain.Assessment;
using Domain.Gamification;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Learning;

public class LearningHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ILeaderboardStore _leaderboard = Substitute.For<ILeaderboardStore>();
    private readonly ILearnerPointsRepository _points = Substitute.For<ILearnerPointsRepository>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static LearnerProfile Profile(CefrLevel level = CefrLevel.B1)
    {
        var placement = new PlacementResult(level, level.ToScore(),
            new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    [Fact]
    public async Task RecordSkillActivity_persists_and_returns_updated_overview()
    {
        var profile = Profile();
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        var handler = new RecordSkillActivityCommandHandler(_profiles, _clock);

        var overview = await handler.Handle(
            new RecordSkillActivityCommand(Learner, SkillType.Speaking, 92,
                new[] { ErrorCategory.Articles }),
            CancellationToken.None);

        overview.SkillScores.Single(s => s.Skill == SkillType.Speaking).Score.Should().Be(92);
        overview.ErrorHeatmap.Single(e => e.Category == ErrorCategory.Articles).Count.Should().Be(1);
        await _profiles.Received(1).SaveAsync(profile, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordSkillActivity_throws_when_profile_missing()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((LearnerProfile?)null);
        var handler = new RecordSkillActivityCommandHandler(_profiles, _clock);

        var act = () => handler.Handle(
            new RecordSkillActivityCommand(Learner, SkillType.Reading, 50), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetLearnerOverview_returns_all_six_skill_scores()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile());
        var handler = new GetLearnerOverviewQueryHandler(_profiles, _clock);

        var overview = await handler.Handle(new GetLearnerOverviewQuery(Learner), CancellationToken.None);

        overview.SkillScores.Should().HaveCount(6);
        overview.OverallLevel.Should().Be(CefrLevel.B1);
    }

    [Fact]
    public async Task GetRecommendations_resolves_uzbek_text_via_template_provider()
    {
        var profile = Profile();
        profile.RecordActivity(SkillType.Speaking, 10, Now); // make Speaking the weakest
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);

        var templates = Substitute.For<IRecommendationTemplateProvider>();
        templates.Resolve(Arg.Any<Recommendation>()).Returns("Gapirish ustida ishlang.");
        var handler = new GetRecommendationsQueryHandler(_profiles, templates, _clock);

        var result = await handler.Handle(new GetRecommendationsQuery(Learner), CancellationToken.None);

        result.Should().Contain(r => r.Code == RecommendationEngine.FocusSkillCode);
        result.First().Text.Should().Be("Gapirish ustida ishlang.");
    }

    [Fact]
    public async Task RecordConfirmationTest_advances_level_when_criteria_met()
    {
        var profile = Profile(CefrLevel.B1);
        foreach (var skill in new[]
                 {
                     SkillType.Speaking, SkillType.Listening, SkillType.Reading, SkillType.Writing
                 })
        {
            profile.RecordActivity(skill, 90, Now);
            profile.RecordActivity(skill, 90, Now.AddMinutes(1));
        }
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        _points.GetOrCreateAsync(Learner, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(LearnerPoints.CreateNew(Learner, Now));
        var handler = new RecordConfirmationTestCommandHandler(_profiles, _leaderboard, _points, _clock);

        var result = await handler.Handle(
            new RecordConfirmationTestCommand(Learner, Passed: true), CancellationToken.None);

        result.LevelAdvanced.Should().BeTrue();
        result.OverallLevel.Should().Be(CefrLevel.B2);
        // Leaderboard/points feature: advancing moves the learner's board entry to the new level.
        await _leaderboard.Received(1).RemoveAsync(Learner, CefrLevel.B1, Arg.Any<CancellationToken>());
        await _leaderboard.Received(1).SetScoreAsync(Learner, CefrLevel.B2, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGrowth_returns_requested_number_of_weeks()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(Profile());
        var handler = new GetGrowthQueryHandler(_profiles, _clock);

        var points = await handler.Handle(new GetGrowthQuery(Learner, Weeks: 6), CancellationToken.None);

        points.Select(p => p.WeekEnding).Distinct().Should().HaveCount(6);
        points.Should().HaveCount(6 * 6);
    }
}
