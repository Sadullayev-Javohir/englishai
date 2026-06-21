using Application.Analytics.Ports;
using Application.Assessment;
using Application.Assessment.FinalizePlacementTest;
using Application.Assessment.Ports;
using Application.Common;
using Application.Learning.Ports;
using Domain.Assessment;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assessment;

public class FinalizePlacementTestCommandHandlerTests
{
    private readonly IPlacementSessionStore _sessions = Substitute.For<IPlacementSessionStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();

    private FinalizePlacementTestCommandHandler CreateHandler() =>
        new(_sessions, _profiles, _events, TimeProvider.System);

    private static PlacementTestSession CompletedSession(Guid learnerId)
    {
        var session = PlacementTestSession.Start(learnerId, includeSpeaking: false);
        while (!session.IsCompleted)
        {
            if (session.IsCurrentStageProductive)
            {
                var taskId = Guid.NewGuid();
                session.ServeItem(taskId);
                session.RecordProductiveResult(taskId, 70, session.CurrentDifficulty);
                continue;
            }

            var remaining = PlacementTestSession.QuestionsPerStage[session.CurrentStage];
            for (var i = 0; i < remaining; i++)
            {
                var questionId = Guid.NewGuid();
                session.ServeItem(questionId);
                session.RecordAnswer(questionId, isCorrect: true);
            }
        }

        return session;
    }

    [Fact]
    public async Task Handle_rejects_incomplete_session()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), includeSpeaking: false);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var act = () => CreateHandler().Handle(
            new FinalizePlacementTestCommand(session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<Domain.Common.DomainException>();
        await _profiles.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task Handle_creates_a_learner_profile_from_the_result()
    {
        var learnerId = Guid.NewGuid();
        var session = CompletedSession(learnerId);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _profiles.GetByLearnerIdAsync(learnerId, Arg.Any<CancellationToken>())
            .Returns((LearnerProfile?)null);

        await CreateHandler().Handle(
            new FinalizePlacementTestCommand(session.Id), CancellationToken.None);

        await _profiles.Received(1).SaveAsync(
            Arg.Is<LearnerProfile>(p => p.LearnerId == learnerId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_when_session_not_found()
    {
        _sessions.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlacementTestSession?)null);

        var act = () => CreateHandler().Handle(
            new FinalizePlacementTestCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<PlacementSessionExpiredException>();
    }

    [Fact]
    public async Task Repeated_finalize_returns_result_without_resetting_the_learners_profile()
    {
        var session = CompletedSession(Guid.NewGuid());
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var handler = CreateHandler();
        var first = await handler.Handle(new(session.Id), CancellationToken.None);
        var second = await handler.Handle(new(session.Id), CancellationToken.None);

        second.Should().BeEquivalentTo(first);
        session.PlacementApplied.Should().BeTrue();
        PlacementTestSession.Restore(session.ToSnapshot()).PlacementApplied.Should().BeTrue();
        await _profiles.Received(1).SaveAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
    }
}
