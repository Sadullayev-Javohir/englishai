using Application.Assessment.Ports;
using Application.Common;
using Application.Learning.Ports;
using Application.Levels.StartLevelExitTest;
using Application.Identity.Dtos;
using Domain.Assessment;
using Domain.Common;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Levels;

public class StartLevelExitTestCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IPlacementSessionStore _sessions = Substitute.For<IPlacementSessionStore>();
    private readonly IPlacementQuestionRepository _questions = Substitute.For<IPlacementQuestionRepository>();
    private readonly IPlacementProductiveTaskProvider _tasks = Substitute.For<IPlacementProductiveTaskProvider>();
    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();

    private static LearnerProfile Profile(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Guid.NewGuid(), placement, Now);
    }

    private StartLevelExitTestCommandHandler Handler() => new(_profiles, _sessions, _questions, _tasks, _admin);

    [Fact]
    public async Task Pins_the_session_to_the_learners_current_level_and_returns_the_first_item()
    {
        var profile = Profile(CefrLevel.B1);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);
        var question = PlacementQuestion.Create(
            Guid.NewGuid(), TestStage.Vocabulary, CefrLevel.B1, "prompt",
            new[] { "a", "b", "c", "d" }, 0);
        _questions.GetNextAsync(
                Arg.Any<TestStage>(), Arg.Any<CefrLevel>(),
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(question);

        var result = await Handler().Handle(
            new StartLevelExitTestCommand(profile.LearnerId), CancellationToken.None);

        result.Level.Should().Be(CefrLevel.B1);
        result.CurrentStage.Should().Be(TestStage.Vocabulary);
        result.FirstItem.Should().NotBeNull();
        await _sessions.Received(1).SaveAsync(
            Arg.Is<PlacementTestSession>(s =>
                s.LearnerId == profile.LearnerId && s.CurrentDifficulty == CefrLevel.B1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_when_the_learner_has_no_profile()
    {
        _profiles.GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((LearnerProfile?)null);

        var act = () => Handler().Handle(
            new StartLevelExitTestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_at_the_highest_level_where_there_is_no_exit_test()
    {
        var profile = Profile(CefrLevel.C2);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);

        var act = () => Handler().Handle(
            new StartLevelExitTestCommand(profile.LearnerId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Super_admin_can_start_another_levels_exit_test()
    {
        var profile = Profile(CefrLevel.B1);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);
        _admin.GetRoleAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(AdminRole.SuperAdmin);
        _questions.GetNextAsync(
                TestStage.Vocabulary, CefrLevel.A1,
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(PlacementQuestion.Create(
                Guid.NewGuid(), TestStage.Vocabulary, CefrLevel.A1, "prompt",
                new[] { "a", "b", "c", "d" }, 0));

        var result = await Handler().Handle(
            new StartLevelExitTestCommand(profile.LearnerId, TestLevel: CefrLevel.A1),
            CancellationToken.None);

        result.Level.Should().Be(CefrLevel.A1);
        result.Requirements.MinimumOverallScore.Should().Be(70);
    }

    [Fact]
    public async Task Ordinary_learner_cannot_start_another_levels_exit_test()
    {
        var profile = Profile(CefrLevel.B1);
        _profiles.GetByLearnerIdAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(profile);
        _admin.GetRoleAsync(profile.LearnerId, Arg.Any<CancellationToken>()).Returns(AdminRole.None);

        var act = () => Handler().Handle(
            new StartLevelExitTestCommand(profile.LearnerId, TestLevel: CefrLevel.A1),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
