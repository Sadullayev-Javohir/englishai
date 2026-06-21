using Application.Assistant.AskAssistant;
using Application.Assistant.Dtos;
using Application.Assistant.Ports;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assistant;

public class AskAssistantQueryHandlerTests
{
    private readonly ILearningAssistant _assistant = Substitute.For<ILearningAssistant>();

    [Fact]
    public async Task Trims_the_question_and_returns_the_assistant_reply()
    {
        _assistant.AnswerAsync("tiger", Arg.Any<IReadOnlyList<AssistantTurn>>(), Arg.Any<CancellationToken>())
            .Returns("tiger - noun (ot). Tarjimasi: yo'lbars.");

        var result = await new AskAssistantQueryHandler(_assistant).Handle(
            new AskAssistantQuery(" tiger ", Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should().Be("tiger - noun (ot). Tarjimasi: yo'lbars.");
    }

    [Fact]
    public async Task Keeps_only_the_last_six_trimmed_history_turns()
    {
        var history = Enumerable.Range(1, 10)
            .Select(i => new AssistantTurnDto(i % 2 == 0 ? "assistant" : "user", $" turn {i} "))
            .ToArray();
        IReadOnlyList<AssistantTurn>? captured = null;
        _assistant.AnswerAsync(
                Arg.Any<string>(),
                Arg.Do<IReadOnlyList<AssistantTurn>>(turns => captured = turns),
                Arg.Any<CancellationToken>())
            .Returns("Javob");

        await new AskAssistantQueryHandler(_assistant).Handle(
            new AskAssistantQuery("Keyingi savol", history), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Select(turn => turn.Text).Should().Equal(
            "turn 5", "turn 6", "turn 7", "turn 8", "turn 9", "turn 10");
    }

    [Fact]
    public async Task Returns_null_when_the_assistant_is_unavailable()
    {
        _assistant.AnswerAsync(
                Arg.Any<string>(), Arg.Any<IReadOnlyList<AssistantTurn>>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await new AskAssistantQueryHandler(_assistant).Handle(
            new AskAssistantQuery("Present Perfect", Array.Empty<AssistantTurnDto>()), CancellationToken.None);

        result.Reply.Should().BeNull();
    }
}
