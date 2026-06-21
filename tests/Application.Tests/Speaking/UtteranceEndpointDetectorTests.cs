using Application.Speaking.AccentTutors;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public sealed class UtteranceEndpointDetectorTests
{
    private readonly IUtteranceSemanticClassifier _classifier = Substitute.For<IUtteranceSemanticClassifier>();

    [Theory]
    [InlineData(700)]
    [InlineData(1200)]
    public async Task Natural_pause_does_not_commit_an_unfinished_thought(int silenceMs)
    {
        var detector = new UtteranceEndpointDetector(_classifier);
        var result = await detector.DetectAsync(Snapshot(silenceMs, "I think that"));
        result.Should().Be(UtteranceEndpointDecision.Wait);
    }

    [Theory]
    [InlineData("I think that…")]
    [InlineData("because I")]
    [InlineData("the reason is")]
    public async Task Explicitly_incomplete_phrases_wait_for_a_short_continuation_window(string text)
    {
        var detector = new UtteranceEndpointDetector(_classifier);
        (await detector.DetectAsync(Snapshot(1_200, text))).Should().Be(UtteranceEndpointDecision.Wait);
        (await detector.DetectAsync(Snapshot(2_200, text))).Should().Be(UtteranceEndpointDecision.Commit);
    }

    [Fact]
    public async Task Stable_complete_sentence_commits_after_the_complete_pause()
    {
        var detector = new UtteranceEndpointDetector(_classifier);
        (await detector.DetectAsync(Snapshot(1_200, "I enjoy learning English.")))
            .Should().Be(UtteranceEndpointDecision.Commit);
    }

    [Fact]
    public async Task Changing_transcript_cannot_commit()
    {
        var detector = new UtteranceEndpointDetector(_classifier);
        var snapshot = Snapshot(2_000, "I enjoy learning English.") with { TranscriptStableFor = TimeSpan.FromMilliseconds(100) };
        (await detector.DetectAsync(snapshot)).Should().Be(UtteranceEndpointDecision.Wait);
    }

    [Theory]
    [InlineData(3000, 5)]
    [InlineData(100, 30)]
    public async Task Hard_limits_force_commit(int silenceMs, int durationSeconds)
    {
        var detector = new UtteranceEndpointDetector(_classifier);
        (await detector.DetectAsync(Snapshot(silenceMs, "because I") with { TurnDuration = TimeSpan.FromSeconds(durationSeconds) }))
            .Should().Be(UtteranceEndpointDecision.Commit);
    }

    [Theory]
    [InlineData(3000, 5)]
    [InlineData(100, 30)]
    public async Task Hard_limits_discard_noise_without_a_transcript(int silenceMs, int durationSeconds)
    {
        var detector = new UtteranceEndpointDetector(_classifier);
        (await detector.DetectAsync(Snapshot(silenceMs, string.Empty) with { TurnDuration = TimeSpan.FromSeconds(durationSeconds) }))
            .Should().Be(UtteranceEndpointDecision.Discard);
    }

    [Fact]
    public async Task Uncertain_text_uses_semantic_classifier()
    {
        _classifier.ClassifyAsync("I went home", Arg.Any<CancellationToken>())
            .Returns(UtteranceSemanticState.Complete);
        var detector = new UtteranceEndpointDetector(_classifier);
        (await detector.DetectAsync(Snapshot(1_500, "I went home"))).Should().Be(UtteranceEndpointDecision.Commit);
    }

    private static UtteranceEndpointSnapshot Snapshot(int silenceMs, string text) => new(
        TimeSpan.FromMilliseconds(silenceMs),
        text,
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(10));
}
