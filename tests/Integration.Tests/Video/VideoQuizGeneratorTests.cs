using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;
using FluentAssertions;
using Infrastructure.Llm;
using Infrastructure.Video;
using NSubstitute;
using Xunit;

namespace Integration.Tests.Video;

public sealed class VideoQuizGeneratorTests
{
    private static readonly TranscriptSegment[] Source = [TranscriptSegment.Create(1, 4, "I'm Tim.", "Men Timman.")];
    private const string Valid = """
        {"questions":[{"prompt":"What is his name?","promptUz":"Uning ismi nima?",
        "options":["Tim","Tom","Sam","Joe"],"correctOptionIndex":0,"sourceIndex":0,
        "evidenceQuote":"I'm Tim.","explanationUz":"U o'zini Tim deb tanishtirdi."}]}
        """;

    [Fact]
    public void Parses_four_choices_and_binds_only_real_source_timestamps()
    {
        var questions = LlmVideoQuizGenerator.Parse(Valid, Source);
        questions.Should().ContainSingle();
        questions[0].SourceStartSeconds.Should().Be(1);
        questions[0].SourceText.Should().Be("I'm Tim.");
        questions[0].Options.Should().HaveCount(4);
    }

    [Theory]
    [InlineData("I'm Tim.", "I'm Bob.")]
    [InlineData("\"sourceIndex\":0", "\"sourceIndex\":99")]
    [InlineData("\"correctOptionIndex\":0", "\"correctOptionIndex\":4")]
    [InlineData("\"Tim\",\"Tom\",\"Sam\",\"Joe\"", "\"Tim\",\"Tim\",\"Sam\",\"Joe\"")]
    [InlineData("\"Tim\",\"Tom\",\"Sam\",\"Joe\"", "\"Tim\",\"Tom\"")]
    [InlineData("Uning ismi nima?", "<script>bad</script>")]
    public void Rejects_ungrounded_or_malformed_AI_questions(string from, string to) =>
        LlmVideoQuizGenerator.Parse(Valid.Replace(from, to), Source).Should().BeEmpty();

    [Fact]
    public void Malformed_payload_is_not_a_quiz() =>
        LlmVideoQuizGenerator.Parse("Not JSON", Source).Should().BeEmpty();

    [Fact]
    public async Task Concurrent_requests_reuse_validated_transcript_scoped_content()
    {
        var llm = Substitute.For<ILlmCompletion>();
        llm.CompleteAsync(Arg.Any<string>(), Arg.Any<string>(), 1200, Arg.Any<CancellationToken>()).Returns(Valid);
        var generator = new LlmVideoQuizGenerator(llm, new InMemoryVideoExplainCache());
        var requests = Enumerable.Range(0, 4).Select(_ => generator.GenerateAsync("Title", CefrLevel.A2, Source, CancellationToken.None));
        (await Task.WhenAll(requests)).Should().OnlyContain(result => result.Count == 1);
        await llm.Received(1).CompleteAsync(Arg.Any<string>(), Arg.Is<string>(s => s.Contains("VIDEO_TRANSCRIPT")), 1200, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_provider_never_fabricates_questions() =>
        (await new LlmVideoQuizGenerator(null, new InMemoryVideoExplainCache()).GenerateAsync("Title", CefrLevel.A2, Source, CancellationToken.None))
        .Should().BeEmpty();
}
