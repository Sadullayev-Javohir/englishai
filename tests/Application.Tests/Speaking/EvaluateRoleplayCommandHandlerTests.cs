using Application.Common;
using Application.Speaking.EvaluateRoleplay;
using Application.Speaking.Ports;
using Domain.Assessment;
using Domain.Speaking;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class EvaluateRoleplayCommandHandlerTests
{
    private readonly IConversationStore _conversations = Substitute.For<IConversationStore>();
    private readonly IRoleplayEvaluator _evaluator = Substitute.For<IRoleplayEvaluator>();
    private readonly IFeedbackTemplateProvider _feedback = Substitute.For<IFeedbackTemplateProvider>();

    public EvaluateRoleplayCommandHandlerTests()
    {
        // Echo the requested code back so tests can assert exactly which template was chosen.
        _feedback.Get(default!).ReturnsForAnyArgs(ci => ci.Arg<string>());
    }

    private EvaluateRoleplayCommandHandler CreateHandler() =>
        new(_conversations, _evaluator, _feedback);

    private static ConversationSession RoleplaySessionWithLearnerTurns()
    {
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, scenarioCode: "restaurant");
        session.AddTutorTurn("Good evening! Can I get you a drink?");
        session.AddLearnerTurn("Yes, I would like some water please.");
        return session;
    }

    [Fact]
    public async Task Handle_throws_NotFound_when_the_session_does_not_exist()
    {
        _conversations.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ConversationSession?)null);

        var act = () => CreateHandler().Handle(
            new EvaluateRoleplayCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_throws_Conflict_when_the_session_is_not_a_roleplay()
    {
        var session = ConversationSession.Start(Guid.NewGuid(), CefrLevel.A2); // no scenario
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var act = () => CreateHandler().Handle(
            new EvaluateRoleplayCommand(session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_returns_a_non_evaluable_result_when_the_learner_never_spoke()
    {
        var session = ConversationSession.Start(
            Guid.NewGuid(), CefrLevel.A2, scenarioCode: "doctor");
        session.AddTutorTurn("What brings you in today?"); // only a tutor turn
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler().Handle(
            new EvaluateRoleplayCommand(session.Id), CancellationToken.None);

        result.Evaluable.Should().BeFalse();
        result.SummaryUz.Should().Be("roleplay.too_short");
        result.OverallScore.Should().Be(0);
        // The evaluator must not be called when there is nothing to grade (no paid LLM call).
        await _evaluator.DidNotReceiveWithAnyArgs()
            .EvaluateAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_maps_scores_and_selects_template_feedback_by_band_and_dimension()
    {
        var session = RoleplaySessionWithLearnerTurns();
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        // Overall 75 -> NeedsImprovement; strongest = TaskCompletion, weakest = Appropriateness.
        _evaluator.EvaluateAsync(session, Arg.Any<RoleplayScenarioDefinition>(), Arg.Any<CancellationToken>())
            .Returns(new RoleplayEvaluation(90, 80, 70, 60));

        var result = await CreateHandler().Handle(
            new EvaluateRoleplayCommand(session.Id), CancellationToken.None);

        result.Evaluable.Should().BeTrue();
        result.ScenarioCode.Should().Be("restaurant");
        result.OverallScore.Should().Be(75.0);
        result.TaskCompletion.Should().Be(90);
        result.Appropriateness.Should().Be(60);
        result.Band.Should().Be(PronunciationBand.NeedsImprovement);
        result.StrongestDimension.Should().Be(RoleplayDimension.TaskCompletion);
        result.WeakestDimension.Should().Be(RoleplayDimension.Appropriateness);
        result.SummaryUz.Should().Be("roleplay.summary.needs_improvement");
        result.StrengthUz.Should().Be("roleplay.strength.task_completion");
        result.TipUz.Should().Be("roleplay.tip.appropriateness");
    }

    [Fact]
    public async Task Handle_uses_the_good_summary_template_for_a_strong_performance()
    {
        var session = RoleplaySessionWithLearnerTurns();
        _conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _evaluator.EvaluateAsync(session, Arg.Any<RoleplayScenarioDefinition>(), Arg.Any<CancellationToken>())
            .Returns(new RoleplayEvaluation(90, 90, 90, 90));

        var result = await CreateHandler().Handle(
            new EvaluateRoleplayCommand(session.Id), CancellationToken.None);

        result.Band.Should().Be(PronunciationBand.Good);
        result.SummaryUz.Should().Be("roleplay.summary.good");
    }
}
