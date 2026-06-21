using Application.Assessment.Ports;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Writing.Ports;
using Domain.Assessment;
using Domain.Speaking;
using Domain.Writing;
using FluentAssertions;
using Infrastructure.Assessment;
using Infrastructure.Speaking;
using Infrastructure.Writing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Assessment;

/// <summary>
/// The placement test spans ~25 requests. Writing retains its structured fallback, while
/// speaking provider failures must be retryable and must never fabricate a score.
/// </summary>
public class PlacementResilienceTests
{
    // ----- ResilientWritingAssessor -----

    private static WritingAssessment Assessment(Guid taskId, int uniformScore) =>
        WritingAssessment.Create(
            taskId,
            new[]
            {
                new DimensionScore(WritingDimension.TaskAchievement, uniformScore),
                new DimensionScore(WritingDimension.Coherence, uniformScore),
                new DimensionScore(WritingDimension.LexicalResource, uniformScore),
                new DimensionScore(WritingDimension.GrammaticalAccuracy, uniformScore),
            },
            Array.Empty<WritingIssue>());

    private static WritingTask SampleTask() =>
        WritingTask.Create("Describe your typical day.", CefrLevel.A2, 20, 40, DateTimeOffset.UtcNow);

    private sealed class StubWritingAssessor : IWritingAssessor
    {
        private readonly Func<Guid, WritingAssessment> _result;
        public bool WasCalled { get; private set; }

        public StubWritingAssessor(Func<Guid, WritingAssessment> result) => _result = result;

        public Task<WritingAssessment> AssessAsync(
            WritingTask task, string text, CefrLevel assessmentLevel, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(_result(task.Id));
        }
    }

    private sealed class ThrowingWritingAssessor : IWritingAssessor
    {
        public Task<WritingAssessment> AssessAsync(
            WritingTask task, string text, CefrLevel assessmentLevel, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("LLM provider unavailable.");
    }

    [Fact]
    public async Task Writing_falls_back_to_the_offline_assessor_when_the_primary_throws()
    {
        var task = SampleTask();
        var fallback = new StubWritingAssessor(id => Assessment(id, 2));
        var resilient = new ResilientWritingAssessor(
            new ThrowingWritingAssessor(), fallback, NullLogger<ResilientWritingAssessor>.Instance);

        var result = await resilient.AssessAsync(
            task, "I wake up early and go to work.", CefrLevel.A2, CancellationToken.None);

        fallback.WasCalled.Should().BeTrue();
        result.OverallPercent.Should().Be(Assessment(task.Id, 2).OverallPercent);
    }

    [Fact]
    public async Task Writing_uses_the_primary_and_skips_the_fallback_when_the_primary_succeeds()
    {
        var task = SampleTask();
        var fallback = new StubWritingAssessor(id => Assessment(id, 1));
        var resilient = new ResilientWritingAssessor(
            new StubWritingAssessor(id => Assessment(id, 5)), fallback, NullLogger<ResilientWritingAssessor>.Instance);

        var result = await resilient.AssessAsync(
            task, "I wake up early and go to work.", CefrLevel.A2, CancellationToken.None);

        fallback.WasCalled.Should().BeFalse();
        result.OverallPercent.Should().Be(Assessment(task.Id, 5).OverallPercent);
    }

    // ----- PlacementSpeakingAssessor -----

    private sealed class ThrowingStt : ISpeechToTextService
    {
        public Task<SpeechTranscription> TranscribeAsync(byte[] audioContent, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Azure STT timed out.");
    }

    private sealed class UnusedPronunciation : IPronunciationAssessor
    {
        public Task<PronunciationResult> AssessAsync(byte[] audioContent, string referenceText, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Should not be reached.");
    }

    private sealed class StubStt(string transcript) : ISpeechToTextService
    {
        public Task<SpeechTranscription> TranscribeAsync(
            byte[] audioContent,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SpeechTranscription.Accepted(transcript, 0.96));
    }

    private sealed class StubPronunciation(double score) : IPronunciationAssessor
    {
        public Task<PronunciationResult> AssessAsync(
            byte[] audioContent, string referenceText, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PronunciationResult(score, score, score, score, Array.Empty<WordPronunciation>()));
    }

    [Fact]
    public async Task Speaking_returns_retryable_unavailable_when_Azure_throws()
    {
        var azure = new AzureSpeechOptions { Key = "k", Region = "eastus" };
        var assessor = new PlacementSpeakingAssessor(
            new ThrowingStt(), new UnusedPronunciation(), azure, NullLogger<PlacementSpeakingAssessor>.Instance);
        var task = PlacementSpeakingTask.Create(Guid.NewGuid(), CefrLevel.B1, "Describe your hometown.", 30);

        var score = await assessor.AssessAsync(new byte[4096], task, CancellationToken.None);

        score.Outcome.Should().Be(PlacementSpeakingOutcome.ServiceUnavailable);
        score.Score.Should().Be(0);
        score.ShouldAdvance.Should().BeFalse();
    }

    [Fact]
    public async Task Speaking_returns_retryable_unavailable_when_Azure_is_not_configured()
    {
        var assessor = new PlacementSpeakingAssessor(
            new StubStt("I wake up early every morning."),
            new StubPronunciation(90),
            new AzureSpeechOptions(),
            NullLogger<PlacementSpeakingAssessor>.Instance);
        var task = PlacementSpeakingTask.Create(Guid.NewGuid(), CefrLevel.A2, "Describe your daily routine.", 20);

        var score = await assessor.AssessAsync(new byte[48_000], task, CancellationToken.None);

        score.Outcome.Should().Be(PlacementSpeakingOutcome.ServiceUnavailable);
        score.Score.Should().Be(0);
        score.ShouldAdvance.Should().BeFalse();
    }

    [Fact]
    public async Task Three_simple_A1_sentences_cannot_be_reported_as_C1()
    {
        var azure = new AzureSpeechOptions { Key = "k", Region = "eastus" };
        var assessor = new PlacementSpeakingAssessor(
            new StubStt("My name is Ali. I am twenty. I live in Tashkent."),
            new StubPronunciation(98),
            azure,
            NullLogger<PlacementSpeakingAssessor>.Instance);
        var task = PlacementSpeakingTask.Create(
            Guid.NewGuid(), CefrLevel.A1, "Tell me about yourself.", 15);

        var score = await assessor.AssessAsync(new byte[48_000], task, CancellationToken.None);

        score.Outcome.Should().Be(PlacementSpeakingOutcome.Scored);
        CefrLevelExtensions.FromScore(score.Score).Should().BeOneOf(CefrLevel.A1, CefrLevel.A2);
    }

    [Fact]
    public async Task Clear_pronunciation_without_enough_language_evidence_stays_below_B2()
    {
        var azure = new AzureSpeechOptions { Key = "k", Region = "eastus" };
        var assessor = new PlacementSpeakingAssessor(
            new StubStt("I like football and I play every day."),
            new StubPronunciation(100),
            azure,
            NullLogger<PlacementSpeakingAssessor>.Instance);
        var task = PlacementSpeakingTask.Create(
            Guid.NewGuid(), CefrLevel.B2, "Describe an important decision.", 40);

        var score = await assessor.AssessAsync(new byte[48_000], task, CancellationToken.None);

        CefrLevelExtensions.FromScore(score.Score).Should().BeOneOf(CefrLevel.A1, CefrLevel.A2);
    }

    [Fact]
    public async Task Fluent_but_unrelated_answer_receives_zero_and_advances()
    {
        var azure = new AzureSpeechOptions { Key = "k", Region = "eastus" };
        var assessor = new PlacementSpeakingAssessor(
            new StubStt("Football is my favourite sport because I play every weekend with my friends."),
            new StubPronunciation(96),
            azure,
            NullLogger<PlacementSpeakingAssessor>.Instance);
        var task = PlacementSpeakingTask.Create(
            Guid.NewGuid(), CefrLevel.A2, "Describe your daily routine. What do you do in the morning, afternoon, and evening?", 20);

        var score = await assessor.AssessAsync(new byte[48_000], task, CancellationToken.None);

        score.Outcome.Should().Be(PlacementSpeakingOutcome.OffTopic);
        score.Score.Should().Be(0);
        score.ShouldAdvance.Should().BeTrue();
    }

    [Fact]
    public async Task Topic_keywords_allow_a_relevant_answer_to_be_scored()
    {
        var azure = new AzureSpeechOptions { Key = "k", Region = "eastus" };
        var assessor = new PlacementSpeakingAssessor(
            new StubStt("I wake up at seven in the morning, eat breakfast, work in the afternoon, and study in the evening."),
            new StubPronunciation(90),
            azure,
            NullLogger<PlacementSpeakingAssessor>.Instance);
        var task = PlacementSpeakingTask.Create(
            Guid.NewGuid(), CefrLevel.A2, "Describe your daily routine. What do you do in the morning, afternoon, and evening?", 20);

        var score = await assessor.AssessAsync(new byte[48_000], task, CancellationToken.None);

        score.Outcome.Should().Be(PlacementSpeakingOutcome.Scored);
        score.Score.Should().BeGreaterThan(0);
    }
}
