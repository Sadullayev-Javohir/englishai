using FluentAssertions;
using Domain.Assessment;
using Domain.Writing;
using Infrastructure.Llm;
using Infrastructure.Writing;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Writing;

/// <summary>
/// Fixture tests for the LLM writing-assessment response parser. Budget models occasionally
/// wrap the JSON in markdown code fences despite the "JSON only" instruction; the parser must
/// stay tolerant (extract the outermost object) rather than throw - which previously surfaced
/// as a 500 on POST /api/placement/answer/writing.
/// </summary>
public class HermesWritingAssessorParseTests
{
    private const string Payload =
        """
        {"dimensions":{"task":4,"coherence":3,"lexical":4,"grammar":3},
         "issues":[{"dimension":"grammar","code":"missing_article","start":0,"end":3,"category":"Articles"}]}
        """;

    [Fact]
    public void Parses_clean_json()
    {
        var parsed = HermesWritingAssessor.Parse(Payload);

        parsed.Dimensions.Should().NotBeNull();
        parsed.Dimensions!.Task.Should().Be(4);
        parsed.Issues.Should().HaveCount(1);
        parsed.Issues![0].Code.Should().Be("missing_article");
    }

    [Fact]
    public void Strips_code_fences_and_prose_around_the_object()
    {
        var text = $"Here is the assessment:\n```json\n{Payload}\n```\nDone.";

        var parsed = HermesWritingAssessor.Parse(text);

        parsed.Dimensions.Should().NotBeNull();
        parsed.Dimensions!.Grammar.Should().Be(3);
    }

    [Fact]
    public void Throws_when_no_json_object_present()
    {
        var act = () => HermesWritingAssessor.Parse("Sorry, I cannot help with that.");

        act.Should().Throw<System.InvalidOperationException>();
    }

    [Fact]
    public async Task Does_not_restart_the_whole_provider_chain_when_it_returns_no_result()
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        var assessor = new HermesWritingAssessor(llm);
        var task = WritingTask.Create(
            "Write about your day.", CefrLevel.A2, 50, 80,
            new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero));

        var act = () => assessor.AssessAsync(task, "I study English every day.", CefrLevel.A2, default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await llm.Received(1).CompleteAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
