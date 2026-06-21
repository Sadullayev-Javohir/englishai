using Application.Assistant.Ports;
using FluentAssertions;
using Infrastructure.Assistant;
using Infrastructure.Llm;
using Xunit;

namespace Integration.Tests.Assistant;

public class HermesLearningAssistantTests
{
    private sealed class QueuedCompletion(params string?[] replies) : ILlmCompletion
    {
        private readonly Queue<string?> _replies = new(replies);
        public List<(string System, string User, int Tokens)> Calls { get; } = new();
        public string Model => "test-hermes";

        public Task<string?> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            int maxOutputTokens,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((systemPrompt, userPrompt, maxOutputTokens));
            return Task.FromResult(_replies.Dequeue());
        }
    }

    private sealed class HangingCompletion : ILlmCompletion
    {
        public string Model => "hanging-provider";

        public async Task<string?> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            int maxOutputTokens,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }
    }

    [Fact]
    public async Task Generates_and_verifies_a_single_word_answer()
    {
        var completion = new QueuedCompletion(
            "{\"intent\":\"word\",\"answer\":\"tiger - noun (ot). Tarjimasi: yo'lbars.\"}",
            "{\"approved\":true,\"answer\":\"tiger - noun (ot). Tarjimasi: yo'lbars.\",\"containsHiddenPracticeAnswers\":false}");

        var result = await new HermesLearningAssistant(completion).AnswerAsync(
            "tiger", Array.Empty<AssistantTurn>(), CancellationToken.None);

        result.Should().Be("tiger - noun (ot). Tarjimasi: yo'lbars.");
        completion.Calls.Should().HaveCount(2);
        completion.Calls[0].System.Should().Contain("part of speech").And.Contain("NOUN=ot");
        completion.Calls[1].System.Should().Contain("second-pass quality reviewer");
        completion.Calls.Should().OnlyContain(call => call.Tokens == 1400);
    }

    [Fact]
    public async Task Sends_history_and_a_grammar_question_as_json_data()
    {
        var completion = new QueuedCompletion(
            "{\"intent\":\"grammar\",\"answer\":\"## Tuzilishi\\n**Present Perfect**: **have/has + V3**.\"}",
            "{\"approved\":true,\"answer\":\"## Tuzilishi\\n**Present Perfect**: **have/has + V3**.\",\"containsHiddenPracticeAnswers\":false}");

        await new HermesLearningAssistant(completion).AnswerAsync(
            "Present Perfectni tushuntirib bering",
            new[] { new AssistantTurn("user", "Oldingi savol") },
            CancellationToken.None);

        completion.Calls[0].User.Should().Contain("Present Perfectni tushuntirib bering");
        completion.Calls[0].User.Should().Contain("Oldingi savol");
        completion.Calls[0].System.Should().Contain("## Qisqa tushuncha").And.Contain("positive, negative, and question formulas");
        completion.Calls[1].System.Should().Contain("selectively bolded").And.Contain("newly generated test/exercise never contains an answer key");
        completion.Calls[1].User.Should().Contain("Oldingi savol");
    }

    [Fact]
    public async Task Accepts_a_practice_without_answers_and_sends_history_to_the_reviewer()
    {
        const string practice = "## Test\\n1. I ___ ready.\\nA) have been\\nB) has been\\nC) been have\\n\\nJavobingizni yuboring.";
        var completion = new QueuedCompletion(
            $"{{\"intent\":\"practice\",\"answer\":\"{practice}\"}}",
            $"{{\"approved\":true,\"answer\":\"{practice}\",\"containsHiddenPracticeAnswers\":false}}");

        var result = await new HermesLearningAssistant(completion).AnswerAsync(
            "Present Perfect boyicha test ber",
            new[] { new AssistantTurn("assistant", "Oldingi tushuntirish") },
            CancellationToken.None);

        result.Should().Contain("A) have been").And.NotContain("Togri javob");
        completion.Calls[1].User.Should().Contain("Oldingi tushuntirish").And.Contain("\"intent\":\"practice\"");
    }

    [Fact]
    public async Task Rejects_a_practice_when_the_reviewer_detects_hidden_answers()
    {
        var completion = new QueuedCompletion(
            "{\"intent\":\"practice\",\"answer\":\"1. I ___ ready. A) have been B) has been\"}",
            "{\"approved\":true,\"answer\":\"1. I ___ ready. A) have been B) has been\",\"containsHiddenPracticeAnswers\":true}");

        var result = await new HermesLearningAssistant(completion).AnswerAsync(
            "Test ber", Array.Empty<AssistantTurn>(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Keeps_a_clean_non_practice_draft_when_the_reviewer_is_unavailable()
    {
        var completion = new QueuedCompletion(
            "{\"intent\":\"word\",\"answer\":\"**tiger** - noun (ot). Tarjimasi: yo'lbars.\"}",
            null);

        var result = await new HermesLearningAssistant(completion).AnswerAsync(
            "tiger", Array.Empty<AssistantTurn>(), CancellationToken.None);

        result.Should().Be("**tiger** - noun (ot). Tarjimasi: yo'lbars.");
    }

    [Theory]
    [InlineData("not json", "{\"approved\":true,\"answer\":\"ok\",\"containsHiddenPracticeAnswers\":false}")]
    [InlineData("{\"intent\":\"word\",\"answer\":\"yo'lbars\"}", "{\"approved\":false,\"answer\":null}")]
    [InlineData("{\"intent\":\"word\",\"answer\":\"yo'lbars\"}", "{\"approved\":true,\"answer\":\"йўлбарс\"}")]
    public async Task Rejects_malformed_unapproved_or_non_ascii_results(string draft, string verified)
    {
        var completion = new QueuedCompletion(draft, verified);

        var result = await new HermesLearningAssistant(completion).AnswerAsync(
            "tiger", Array.Empty<AssistantTurn>(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Hanging_provider_returns_null_inside_the_regular_frontend_timeout()
    {
        var assistant = new HermesLearningAssistant(
            new HangingCompletion(), requestTimeout: TimeSpan.FromMilliseconds(50));

        var result = await assistant.AnswerAsync("tiger", Array.Empty<AssistantTurn>(), CancellationToken.None);

        result.Should().BeNull();
    }
}
