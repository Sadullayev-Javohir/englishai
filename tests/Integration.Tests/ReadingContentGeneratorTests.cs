using Application.Reading.Models;
using Domain.Assessment;
using Infrastructure.Reading;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Verifies the topic-scoped reading generators: the offline Local generator produces a complete,
/// quiz-ready lesson for every CEFR level, and the Claude generator's parser reads valid JSON and
/// rejects malformed payloads (so a bad LLM reply leaves the lesson honestly pending).
/// </summary>
public class ReadingContentGeneratorTests
{
    private static readonly CefrLevel[] Levels =
        { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 };

    [Fact]
    public async Task LocalGenerator_produces_a_complete_lesson_for_each_level()
    {
        var generator = new LocalReadingContentGenerator();

        foreach (var level in Levels)
        {
            var content = await generator.GenerateAsync("Why We Sleep", level);

            content.HasContent.Should().BeTrue();
            content.Glossary.Should().NotBeEmpty();
            content.Questions.Should().NotBeEmpty();

            // Every question is answerable: the correct index is in range and points at a real option.
            content.Questions.Should().OnlyContain(q =>
                q.Options.Count >= 2 &&
                q.CorrectOptionIndex >= 0 &&
                q.CorrectOptionIndex < q.Options.Count);

            // Glossary words appear in the passage body so tap-to-translate lines up with the text.
            content.Glossary.Should().OnlyContain(g => content.Body.Contains(g.Word));
        }
    }

    [Fact]
    public void ClaudeParser_reads_valid_json_and_rejects_garbage()
    {
        const string json =
            """
            Sure: {"body":"Sleep is essential. Rest helps the body recover.",
              "glossary":[{"w":"essential","uz":"zarur","ex":"Sleep is essential."}],
              "questions":[{"q":"Why is sleep important?","options":["It helps the body","It is boring"],"answer":0,"why":"The text says rest helps the body."}]}
            """;

        var parsed = LlmReadingContentGenerator.Parse(json);

        parsed.HasContent.Should().BeTrue();
        parsed.Glossary.Should().ContainSingle(g => g.Word == "essential" && g.Translation == "zarur");
        parsed.Questions.Should().ContainSingle(q => q.CorrectOptionIndex == 0 && q.Explanation != null);

        LlmReadingContentGenerator.Parse("not json at all").Should().Be(GeneratedReadingContent.Empty);
        // A passage with no questions is unusable, so it parses to Empty (lesson stays pending).
        LlmReadingContentGenerator.Parse("{\"body\":\"Text.\",\"questions\":[]}").HasContent.Should().BeFalse();
    }
}
