using FluentAssertions;
using Infrastructure.Assistant;
using Xunit;

namespace Integration.Tests.Assistant;

public sealed class LocalContextualAssistantTests
{
    [Fact]
    public async Task Exact_platform_word_returns_structured_dictionary_answer()
    {
        var assistant = new LocalContextualAssistant();
        var context = """
            PLATFORM KNOWLEDGE:
            Words:
            - mother | ona | part of speech: Noun | example: My mother is a teacher. | usage: Family members haqida gapirganda ishlatiladi.
            """;

        var answer = await assistant.AnswerAsync("vocabulary", "Family", context, "", "mother", []);

        answer.Should().Contain("Tarjima").And.Contain("ona");
        answer.Should().Contain("So'z turkumi").And.Contain("Noun");
        answer.Should().Contain("My mother is a teacher.");
    }
}
