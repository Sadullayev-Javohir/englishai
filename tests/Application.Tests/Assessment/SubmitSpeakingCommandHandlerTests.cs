using Application.Assessment.Ports;
using Application.Assessment.SubmitSpeaking;
using Domain.Assessment;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assessment;

public class SubmitSpeakingCommandHandlerTests
{
    private readonly IPlacementSessionStore _sessions = Substitute.For<IPlacementSessionStore>();
    private readonly IPlacementQuestionRepository _questions = Substitute.For<IPlacementQuestionRepository>();
    private readonly IPlacementProductiveTaskProvider _tasks = Substitute.For<IPlacementProductiveTaskProvider>();
    private readonly IPlacementSpeakingAssessor _assessor = Substitute.For<IPlacementSpeakingAssessor>();

    private SubmitSpeakingCommandHandler CreateHandler() =>
        new(_sessions, _questions, _tasks, _assessor);

    private static PlacementTestSession SpeakingSession(Guid taskId)
    {
        var session = PlacementTestSession.Start(Guid.NewGuid(), includeSpeaking: true);
        while (session.CurrentStage != TestStage.Speaking)
        {
            if (session.IsCurrentStageProductive)
            {
                var currentTaskId = Guid.NewGuid();
                session.ServeItem(currentTaskId);
                session.RecordProductiveResult(currentTaskId, 70, session.CurrentDifficulty);
                continue;
            }

            var remaining = PlacementTestSession.QuestionsPerStage[session.CurrentStage];
            for (var index = 0; index < remaining; index++)
            {
                var questionId = Guid.NewGuid();
                session.ServeItem(questionId);
                session.RecordAnswer(questionId, isCorrect: true);
            }
        }

        session.ServeItem(taskId);
        return session;
    }

    [Fact]
    public async Task Retryable_outcome_does_not_advance_or_save_session()
    {
        var task = PlacementSpeakingTask.Create(Guid.NewGuid(), CefrLevel.B1, "Talk about a hobby you enjoy.", 30);
        var session = SpeakingSession(task.Id);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _tasks.GetSpeakingTask(session.CurrentDifficulty).Returns(task);
        _assessor.AssessAsync(Arg.Any<byte[]>(), task, Arg.Any<CancellationToken>())
            .Returns(new PlacementSpeakingScore(0, PlacementSpeakingOutcome.ServiceUnavailable));

        var result = await CreateHandler().Handle(
            new SubmitSpeakingCommand(session.Id, task.Id, new byte[3_200]), CancellationToken.None);

        result.Retryable.Should().BeTrue();
        result.Outcome.Should().Be(PlacementSpeakingOutcome.ServiceUnavailable);
        result.NextItem.Should().BeNull();
        session.CurrentItemId.Should().Be(task.Id);
        await _sessions.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task Off_topic_outcome_records_zero_and_advances()
    {
        var task = PlacementSpeakingTask.Create(Guid.NewGuid(), CefrLevel.B1, "Talk about a hobby you enjoy.", 30);
        var session = SpeakingSession(task.Id);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _tasks.GetSpeakingTask(session.CurrentDifficulty).Returns(task);
        _assessor.AssessAsync(Arg.Any<byte[]>(), task, Arg.Any<CancellationToken>())
            .Returns(new PlacementSpeakingScore(0, PlacementSpeakingOutcome.OffTopic));

        var result = await CreateHandler().Handle(
            new SubmitSpeakingCommand(session.Id, task.Id, new byte[3_200]), CancellationToken.None);

        result.Retryable.Should().BeFalse();
        result.Outcome.Should().Be(PlacementSpeakingOutcome.OffTopic);
        result.Score.Should().Be(0);
        session.ProductiveResults.Should().Contain(result =>
            result.Stage == TestStage.Speaking && result.Score == 0);
        await _sessions.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
    }
}
