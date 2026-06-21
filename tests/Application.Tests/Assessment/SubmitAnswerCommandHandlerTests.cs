using Application.Assessment;
using Application.Assessment.Ports;
using Application.Assessment.SubmitAnswer;
using Application.Common;
using Domain.Assessment;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assessment;

public class SubmitAnswerCommandHandlerTests
{
    private readonly IPlacementSessionStore _sessions = Substitute.For<IPlacementSessionStore>();
    private readonly IPlacementQuestionRepository _questions = Substitute.For<IPlacementQuestionRepository>();
    private readonly IPlacementProductiveTaskProvider _tasks = Substitute.For<IPlacementProductiveTaskProvider>();

    private static PlacementQuestion Question(int correctIndex) =>
        PlacementQuestion.Create(
            Guid.NewGuid(), TestStage.Vocabulary, CefrLevel.A2,
            "Sample prompt", new[] { "a", "b", "c", "d" }, correctIndex);

    // Options are shuffled per (session, question), so the index the learner clicks is
    // a display position. This finds the display position that points at a given
    // original option index, mirroring what the browser would send.
    private static int DisplayIndexFor(Guid sessionId, PlacementQuestion question, int originalIndex) =>
        Array.IndexOf(OptionShuffle.Order(sessionId, question.Id, question.Options.Count), originalIndex);

    [Fact]
    public async Task Handle_records_correct_answer_and_returns_next_question()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        var question = Question(correctIndex: 2);
        var nextQuestion = Question(correctIndex: 0);
        session.ServeItem(question.Id);

        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _questions.GetByIdAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);
        _questions.GetNextAsync(
                Arg.Any<TestStage>(), Arg.Any<CefrLevel>(),
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(nextQuestion);

        var handler = new SubmitAnswerCommandHandler(_sessions, _questions, _tasks);

        var result = await handler.Handle(
            new SubmitAnswerCommand(session.Id, question.Id, DisplayIndexFor(session.Id, question, 2)),
            CancellationToken.None);

        result.WasCorrect.Should().BeTrue();
        result.IsTestCompleted.Should().BeFalse();
        result.NextItem.Should().NotBeNull();
        result.NextItem!.Id.Should().Be(nextQuestion.Id);
        await _sessions.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_marks_wrong_answer_as_incorrect()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        var question = Question(correctIndex: 2);
        session.ServeItem(question.Id);

        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _questions.GetByIdAsync(question.Id, Arg.Any<CancellationToken>()).Returns(question);
        _questions.GetNextAsync(
                Arg.Any<TestStage>(), Arg.Any<CefrLevel>(),
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Question(0));

        var handler = new SubmitAnswerCommandHandler(_sessions, _questions, _tasks);

        // Pick the display position of a wrong original option (0, when correct is 2).
        var result = await handler.Handle(
            new SubmitAnswerCommand(session.Id, question.Id, DisplayIndexFor(session.Id, question, 0)),
            CancellationToken.None);

        result.WasCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_throws_when_session_not_found()
    {
        _sessions.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlacementTestSession?)null);

        var handler = new SubmitAnswerCommandHandler(_sessions, _questions, _tasks);

        var act = () => handler.Handle(
            new SubmitAnswerCommand(Guid.NewGuid(), Guid.NewGuid(), 0), CancellationToken.None);

        await act.Should().ThrowAsync<PlacementSessionExpiredException>();
    }

    [Fact]
    public async Task Handle_throws_when_question_not_found()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _questions.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlacementQuestion?)null);

        var handler = new SubmitAnswerCommandHandler(_sessions, _questions, _tasks);

        var act = () => handler.Handle(
            new SubmitAnswerCommand(session.Id, Guid.NewGuid(), 0), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
