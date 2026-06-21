using Application.Assessment.Ports;
using Application.Common;
using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Levels.FinalizeLevelExitTest;
using Application.Identity.Dtos;
using Application.Tests.Learning;
using Domain.Assessment;
using Domain.Gamification;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Levels;

public class FinalizeLevelExitTestCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IPlacementSessionStore _sessions = Substitute.For<IPlacementSessionStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ILeaderboardStore _leaderboard = Substitute.For<ILeaderboardStore>();
    private readonly ILearnerPointsRepository _points = Substitute.For<ILearnerPointsRepository>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);
    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();

    private FinalizeLevelExitTestCommandHandler Handler() => new(_sessions, _profiles, _leaderboard, _points, _clock, admin: _admin);

    private static LearnerProfile Profile(CefrLevel level, bool allSkillsMastered)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        var profile = LearnerProfile.CreateFromPlacement(Guid.NewGuid(), placement, Now);
        if (allSkillsMastered)
            foreach (var skill in Enum.GetValues<SkillType>())
            {
                profile.RecordActivity(skill, 95, Now);
                profile.RecordActivity(skill, 95, Now.AddMinutes(1));
            }
        return profile;
    }

    /// <summary>A completed session whose answers were all correct (a strong pass) or all wrong.</summary>
    private static PlacementTestSession Session(Guid learnerId, bool correct, CefrLevel pinnedAt)
    {
        var session = PlacementTestSession.Start(learnerId, includeSpeaking: true, pinnedAt);
        while (!session.IsCompleted)
        {
            if (session.IsCurrentStageProductive)
            {
                var taskId = Guid.NewGuid();
                session.ServeItem(taskId);
                session.RecordProductiveResult(taskId, correct ? 90 : 10, session.CurrentDifficulty);
                continue;
            }

            var quota = PlacementTestSession.QuestionsPerStage[session.CurrentStage];
            for (var i = 0; i < quota; i++)
            {
                var questionId = Guid.NewGuid();
                session.ServeItem(questionId);
                session.RecordAnswer(questionId, isCorrect: correct);
            }
        }
        return session;
    }

    [Fact]
    public async Task Passing_with_the_skill_bar_met_advances_the_level()
    {
        var profile = Profile(CefrLevel.A2, allSkillsMastered: true);
        var session = Session(profile.LearnerId, correct: true, pinnedAt: CefrLevel.A2);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);
        _points.GetOrCreateAsync(profile.LearnerId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(LearnerPoints.CreateNew(profile.LearnerId, Now));

        var result = await Handler().Handle(
            new FinalizeLevelExitTestCommand(session.Id), CancellationToken.None);

        result.Passed.Should().BeTrue();
        result.Advanced.Should().BeTrue();
        result.TestedLevel.Should().Be(CefrLevel.A2);
        result.NewLevel.Should().Be(CefrLevel.B1);
        profile.OverallLevel.Should().Be(CefrLevel.B1);
        await _profiles.Received(1).SaveAsync(profile, Arg.Any<CancellationToken>());
        // Leaderboard/points feature: advancing moves the learner's board entry to the new level.
        await _leaderboard.Received(1).RemoveAsync(profile.LearnerId, CefrLevel.A2, Arg.Any<CancellationToken>());
        await _leaderboard.Received(1).SetScoreAsync(profile.LearnerId, CefrLevel.B1, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passing_without_the_skill_bar_records_the_confirmation_but_does_not_advance()
    {
        var profile = Profile(CefrLevel.A2, allSkillsMastered: false);
        var session = Session(profile.LearnerId, correct: true, pinnedAt: CefrLevel.A2);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await Handler().Handle(
            new FinalizeLevelExitTestCommand(session.Id), CancellationToken.None);

        result.Passed.Should().BeTrue();
        result.Advanced.Should().BeFalse();
        result.NewLevel.Should().Be(CefrLevel.A2);
        result.MasteredSkillCount.Should().BeLessThan(result.RequiredMasteredSkills);
        profile.OverallLevel.Should().Be(CefrLevel.A2);
        // The confirmation gate is now satisfied, so daily practice that lifts the skills will
        // later advance the level without retaking the test.
        profile.LevelStatus(Now).ConfirmationTestPassed.Should().BeTrue();
    }

    [Fact]
    public async Task Retrying_finalize_after_advancement_does_not_advance_twice()
    {
        var profile = Profile(CefrLevel.A2, allSkillsMastered: true);
        var session = Session(profile.LearnerId, correct: true, pinnedAt: CefrLevel.A2);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);
        _points.GetOrCreateAsync(profile.LearnerId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(LearnerPoints.CreateNew(profile.LearnerId, Now));

        var first = await Handler().Handle(
            new FinalizeLevelExitTestCommand(session.Id), CancellationToken.None);
        var retried = await Handler().Handle(
            new FinalizeLevelExitTestCommand(session.Id), CancellationToken.None);

        first.NewLevel.Should().Be(CefrLevel.B1);
        retried.Advanced.Should().BeTrue();
        retried.NewLevel.Should().Be(CefrLevel.B1);
        profile.OverallLevel.Should().Be(CefrLevel.B1);
        await _profiles.Received(1).SaveAsync(profile, Arg.Any<CancellationToken>());
        await _leaderboard.Received(2).RemoveAsync(profile.LearnerId, CefrLevel.A2, Arg.Any<CancellationToken>());
        await _leaderboard.Received(2).SetScoreAsync(profile.LearnerId, CefrLevel.B1, 0, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failing_does_not_pass_advance_or_record_the_confirmation()
    {
        var profile = Profile(CefrLevel.B1, allSkillsMastered: true);
        var session = Session(profile.LearnerId, correct: false, pinnedAt: CefrLevel.B1);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);

        var result = await Handler().Handle(
            new FinalizeLevelExitTestCommand(session.Id), CancellationToken.None);

        result.Passed.Should().BeFalse();
        result.Advanced.Should().BeFalse();
        result.NewLevel.Should().Be(CefrLevel.B1);
        profile.OverallLevel.Should().Be(CefrLevel.B1);
        profile.LevelStatus(Now).ConfirmationTestPassed.Should().BeFalse();
    }

    [Fact]
    public async Task Super_admin_preview_does_not_change_the_real_profile()
    {
        var profile = Profile(CefrLevel.B1, allSkillsMastered: true);
        var session = Session(profile.LearnerId, correct: true, pinnedAt: CefrLevel.A1);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);
        _admin.GetRoleAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);

        var result = await Handler().Handle(
            new FinalizeLevelExitTestCommand(session.Id), CancellationToken.None);

        result.TestedLevel.Should().Be(CefrLevel.A1);
        result.Advanced.Should().BeFalse();
        profile.OverallLevel.Should().Be(CefrLevel.B1);
        await _profiles.DidNotReceive().SaveAsync(profile, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_the_session_is_not_found()
    {
        _sessions.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlacementTestSession?)null);

        var act = () => Handler().Handle(
            new FinalizeLevelExitTestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
