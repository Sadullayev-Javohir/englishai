using Application.Writing.Models;
using Infrastructure.Llm;
using Domain.Assessment;
using Infrastructure.Writing;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Verifies the topic-scoped writing-prompt generators: the offline Local generator produces a
/// usable prompt + guidance for every CEFR level, and the Claude generator's parser reads valid
/// JSON and rejects malformed payloads. Transport failures propagate to the learner-facing retry path.
/// </summary>
public class WritingPromptGeneratorTests
{
    private static readonly CefrLevel[] Levels =
        { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 };

    [Fact]
    public async Task LocalGenerator_produces_a_prompt_and_guidance_for_each_level()
    {
        var generator = new LocalWritingPromptGenerator();

        foreach (var level in Levels)
        {
            var content = await generator.GenerateAsync("A Busy Morning", level);

            content.HasContent.Should().BeTrue();
            content.Prompt.Should().Contain("A Busy Morning");
            content.Guidance.Should().NotBeEmpty();
        }
    }

    [Fact]
    public async Task LocalGenerator_varies_the_genre_across_topics_at_the_same_level()
    {
        var generator = new LocalWritingPromptGenerator();
        var titles = new[]
        {
            "A Busy Morning", "My Favourite Food", "A Trip to the City", "Learning a New Skill",
            "Weekend Plans", "A Person I Admire", "Health and Exercise", "Technology at Home",
        };

        var prompts = new List<string>();
        foreach (var title in titles)
        {
            var content = await generator.GenerateAsync(title, CefrLevel.A2);
            prompts.Add(content.Prompt);
        }

        // The same topic always maps to the same genre (stable for caching), but across different
        // topics more than one genre is used - no longer a single "short message to a friend".
        prompts.Distinct().Count().Should().BeGreaterThan(1);

        var repeat = await generator.GenerateAsync("A Busy Morning", CefrLevel.A2);
        repeat.Prompt.Should().Be(prompts[0]);
    }

    [Fact]
    public void ClaudeParser_reads_valid_json_and_rejects_garbage()
    {
        const string json =
            """
            Sure: {"prompt":"Write a short message about your busy morning.",
              "guidance":["Use simple sentences.","Say how you felt."]}
            """;

        var parsed = HermesWritingPromptGenerator.Parse(json);

        parsed.HasContent.Should().BeTrue();
        parsed.Prompt.Should().Be("Write a short message about your busy morning.");
        parsed.Guidance.Should().HaveCount(2);

        HermesWritingPromptGenerator.Parse("not json at all").Should().Be(GeneratedWritingPrompt.Empty);
        // A payload with no prompt is unusable, so it parses to Empty (task stays pending).
        HermesWritingPromptGenerator.Parse("{\"guidance\":[\"x\"]}").HasContent.Should().BeFalse();
    }

    [Fact]
    public async Task HermesGenerator_propagates_provider_failure()
    {
        var completion = Substitute.For<ILlmCompletion>();
        completion.CompleteAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns<string?>(_ => throw new InvalidOperationException("Hermes unavailable."));
        var generator = new HermesWritingPromptGenerator(completion);

        var act = () => generator.GenerateAsync("A Busy Morning", CefrLevel.A2);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
