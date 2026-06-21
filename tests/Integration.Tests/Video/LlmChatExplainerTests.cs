using Application.Video.Ports;
using FluentAssertions;
using Infrastructure.Llm;
using Infrastructure.Video;
using Xunit;

namespace Integration.Tests.Video;

public class LlmChatExplainerTests
{
    private sealed class CapturingCompletion : ILlmCompletion
    {
        public string Model => "test-model";
        public string? SystemPrompt { get; private set; }
        public string? UserPrompt { get; private set; }
        public int MaxOutputTokens { get; private set; }
        public string? Reply { get; init; } = "  Video odatlar haqida.  ";

        public Task<string?> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            int maxOutputTokens,
            CancellationToken cancellationToken = default)
        {
            SystemPrompt = systemPrompt;
            UserPrompt = userPrompt;
            MaxOutputTokens = maxOutputTokens;
            return Task.FromResult(Reply);
        }
    }

    [Fact]
    public async Task Sends_full_transcript_and_current_focus_in_separate_prompt_sections()
    {
        var completion = new CapturingCompletion();
        var explainer = new LlmChatExplainer(completion);

        var result = await explainer.ExplainAsync(
            "How habits change",
            "Habits begin with a cue.\nSmall actions become routines.",
            "Small actions become routines.",
            "Bu video nima haqida?",
            new[] { new ChatTurn("user", "Oldingi savol") },
            CancellationToken.None);

        result.Should().Be("Video odatlar haqida.");
        completion.SystemPrompt.Should().Contain("use the whole").And.Contain("FULL_TRANSCRIPT");
        completion.UserPrompt.Should().Contain("VIDEO_TITLE: How habits change");
        completion.UserPrompt.Should().Contain("FULL_TRANSCRIPT:\nHabits begin with a cue.\nSmall actions become routines.");
        completion.UserPrompt.Should().Contain("CURRENT_FOCUS: Small actions become routines.");
        completion.UserPrompt.Should().Contain("QUESTION: Bu video nima haqida?");
        completion.MaxOutputTokens.Should().Be(220);
    }

    [Theory]
    [InlineData("Bu video odatlar haqida. Habit sozining ma'nosi odat.", true)]
    [InlineData("Бу видео одатлар хакида.", false)]
    [InlineData("Bu video عادات haqida.", false)]
    [InlineData("Bu video 习惯 haqida.", false)]
    [InlineData("Bu vidéo odatlar haqida.", false)]
    public async Task Rejects_any_reply_outside_English_and_Uzbek_ASCII_Latin(string reply, bool accepted)
    {
        var completion = new CapturingCompletion { Reply = reply };
        var explainer = new LlmChatExplainer(completion);

        var result = await explainer.ExplainAsync(
            "Habits", "Habits become routines.", "Habits become routines.",
            "Bu gapni tushuntiring.", Array.Empty<ChatTurn>(), CancellationToken.None);

        if (accepted)
            result.Should().Be(reply);
        else
            result.Should().BeNull();
    }

    [Theory]
    // A good Uzbek answer whose only sin is typography used to be thrown away wholesale, leaving the
    // learner with "AI hozir javob bera olmadi" (observed 2026-07-28). Fold it to ASCII and keep it.
    [InlineData("Bu so\u02BBz \u201Chabit\u201D \u2014 odat degani\u2026",
                "Bu so'z \"habit\" - odat degani...")]
    [InlineData("O\u02BBrganish uchun g\u02BBoya kerak.", "O'rganish uchun g'oya kerak.")]
    public async Task Normalizes_typographic_characters_instead_of_discarding_the_whole_reply(
        string reply, string expected)
    {
        var completion = new CapturingCompletion { Reply = reply };
        var explainer = new LlmChatExplainer(completion);

        var result = await explainer.ExplainAsync(
            "Habits", "Habits become routines.", "Habits become routines.",
            "Bu gapni tushuntiring.", Array.Empty<ChatTurn>(), CancellationToken.None);

        result.Should().Be(expected);
    }
}
