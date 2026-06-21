using Application.Speaking.AssessSegmentPronunciation;
using Application.Speaking.Ports;
using Domain.Speaking;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

/// <summary>
/// Verifies the shadowing per-segment scorer: it assesses the clip against the segment's own known
/// reference text (no STT pass needed first, unlike SubmitUtterance), and returns an honest zero
/// score rather than calling the assessor or fabricating a result when no audio was captured.
/// </summary>
public class AssessSegmentPronunciationCommandHandlerTests
{
    private readonly IPronunciationAssessor _assessor = Substitute.For<IPronunciationAssessor>();

    private AssessSegmentPronunciationCommandHandler Handler() => new(_assessor);

    [Fact]
    public async Task Assesses_the_clip_against_the_known_reference_text()
    {
        var domainResult = new PronunciationResult(
            88, 90, 85, 92,
            new[] { new WordPronunciation("hello", 90, PronunciationErrorType.None) });
        _assessor.AssessAsync(Arg.Any<byte[]>(), "Hello there.", Arg.Any<CancellationToken>())
            .Returns(domainResult);

        var result = await Handler().Handle(
            new AssessSegmentPronunciationCommand("Hello there.", new byte[] { 1, 2, 3 }),
            CancellationToken.None);

        result.OverallScore.Should().Be(88);
        result.Words.Should().ContainSingle(w => w.Word == "hello");
        await _assessor.Received(1).AssessAsync(Arg.Any<byte[]>(), "Hello there.", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_a_zero_score_without_calling_the_assessor_when_no_audio_was_captured()
    {
        var result = await Handler().Handle(
            new AssessSegmentPronunciationCommand("Hello there.", Array.Empty<byte>()),
            CancellationToken.None);

        result.OverallScore.Should().Be(0);
        result.Words.Should().BeEmpty();
        await _assessor.DidNotReceiveWithAnyArgs().AssessAsync(default!, default!, default);
    }

    [Fact]
    public async Task Normalizes_numeric_reference_and_returns_the_original_display_token()
    {
        var domainResult = new PronunciationResult(
            70, 70, 75, 100,
            new[]
            {
                new WordPronunciation("five", 65, PronunciationErrorType.Mispronunciation),
                new WordPronunciation("o'clock", 60, PronunciationErrorType.Mispronunciation),
            });
        _assessor.AssessAsync(Arg.Any<byte[]>(), "five o'clock", Arg.Any<CancellationToken>())
            .Returns(domainResult);

        var result = await Handler().Handle(
            new AssessSegmentPronunciationCommand("5:00", new byte[] { 1 }),
            CancellationToken.None);

        result.Words.Should().ContainSingle();
        result.Words[0].Word.Should().Be("5:00");
        result.Words[0].SpokenForm.Should().Be("five o'clock");
    }
}
