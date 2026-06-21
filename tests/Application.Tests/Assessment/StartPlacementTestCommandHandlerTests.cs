using Application.Analytics.Ports;
using Application.Assessment.Ports;
using Application.Assessment.StartPlacementTest;
using Domain.Analytics;
using Domain.Assessment;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assessment;

public class StartPlacementTestCommandHandlerTests
{
    private readonly IPlacementSessionStore _sessions = Substitute.For<IPlacementSessionStore>();
    private readonly IPlacementQuestionRepository _questions = Substitute.For<IPlacementQuestionRepository>();
    private readonly IPlacementProductiveTaskProvider _tasks = Substitute.For<IPlacementProductiveTaskProvider>();

    private static PlacementQuestion SampleQuestion(TestStage stage, CefrLevel level) =>
        PlacementQuestion.Create(
            Guid.NewGuid(), stage, level, "Sample prompt",
            new[] { "a", "b", "c", "d" }, 0);

    [Fact]
    public async Task Handle_starts_session_persists_it_and_returns_first_question()
    {
        var question = SampleQuestion(TestStage.Vocabulary, CefrLevel.A2);
        _questions.GetNextAsync(
                Arg.Any<TestStage>(), Arg.Any<CefrLevel>(),
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(question);

        var events = Substitute.For<IProductEventStore>();
        var handler = new StartPlacementTestCommandHandler(
            _sessions, _questions, _tasks, events, TimeProvider.System);

        var learnerId = Guid.NewGuid();
        var result = await handler.Handle(
            new StartPlacementTestCommand(learnerId), CancellationToken.None);

        result.SessionId.Should().NotBeEmpty();
        result.CurrentStage.Should().Be(TestStage.Vocabulary);
        result.FirstItem.Should().NotBeNull();
        result.FirstItem!.Id.Should().Be(question.Id);
        await _sessions.Received(1).SaveAsync(Arg.Any<PlacementTestSession>(), Arg.Any<CancellationToken>());
        await events.Received(1).AppendOnceAsync(
            learnerId,
            ProductEventType.PlacementStarted,
            Arg.Any<DateTimeOffset>(),
            result.SessionId.ToString(),
            Arg.Any<CancellationToken>());
    }
}
