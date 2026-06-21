using Application.Common;
using Application.Speaking.Ports;
using Application.Speaking.PracticeWords;
using Domain.Speaking;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class SubmitSpeakingPracticeAttemptCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 2, 10, 0, 0, TimeSpan.Zero);
    private readonly ISpeakingPracticeWordRepository _repository = Substitute.For<ISpeakingPracticeWordRepository>();
    private readonly IPronunciationAssessor _assessor = Substitute.For<IPronunciationAssessor>();
    private readonly IFeedbackTemplateProvider _feedback = Substitute.For<IFeedbackTemplateProvider>();

    [Fact]
    public async Task A_single_success_advances_progress_without_mastering_the_word()
    {
        var handler = CreateHandler(word: out var word, score: 85);

        var result = await handler.Handle(
            new SubmitSpeakingPracticeAttemptCommand(word.Id, new byte[] { 1 }), CancellationToken.None);

        result.Mastered.Should().BeFalse();
        result.SuccessfulAttempts.Should().Be(1);
        result.RequiredSuccesses.Should().Be(3);
        word.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Third_success_masters_the_word()
    {
        var handler = CreateHandler(word: out var word, score: 90);
        var command = new SubmitSpeakingPracticeAttemptCommand(word.Id, new byte[] { 1 });

        await handler.Handle(command, CancellationToken.None);
        await handler.Handle(command, CancellationToken.None);
        var result = await handler.Handle(command, CancellationToken.None);

        result.Mastered.Should().BeTrue();
        result.SuccessfulAttempts.Should().Be(3);
        word.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Score_below_boundary_does_not_advance_progress()
    {
        var handler = CreateHandler(word: out var word, score: 84.9);

        var result = await handler.Handle(
            new SubmitSpeakingPracticeAttemptCommand(word.Id, new byte[] { 1 }), CancellationToken.None);

        result.Mastered.Should().BeFalse();
        result.SuccessfulAttempts.Should().Be(0);
        word.IsActive.Should().BeTrue();
    }

    private SubmitSpeakingPracticeAttemptCommandHandler CreateHandler(out SpeakingPracticeWord word, double score)
    {
        word = SpeakingPracticeWord.Create(
            Guid.NewGuid(), "hello", 50, PronunciationErrorType.Mispronunciation, Now);
        _repository.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _assessor.AssessAsync(Arg.Any<byte[]>(), "hello", Arg.Any<CancellationToken>()).Returns(
            new PronunciationResult(score, score, score, score,
                new[] { new WordPronunciation("hello", score, PronunciationErrorType.None) },
                isAuthentic: true));
        return new SubmitSpeakingPracticeAttemptCommandHandler(
            _repository, _assessor, _feedback, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Unrecognized_audio_does_not_update_the_word()
    {
        var word = SpeakingPracticeWord.Create(
            Guid.NewGuid(), "hello", 50, PronunciationErrorType.Mispronunciation, Now);
        _repository.GetByIdAsync(word.Id, Arg.Any<CancellationToken>()).Returns(word);
        _assessor.AssessAsync(Arg.Any<byte[]>(), "hello", Arg.Any<CancellationToken>()).Returns(
            new PronunciationResult(0, 0, 0, 0, Array.Empty<WordPronunciation>(), isAuthentic: false));
        var handler = new SubmitSpeakingPracticeAttemptCommandHandler(
            _repository, _assessor, _feedback, new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new SubmitSpeakingPracticeAttemptCommand(word.Id, new byte[] { 1 }), CancellationToken.None);

        result.Mastered.Should().BeFalse();
        await _repository.DidNotReceive().SaveAsync(word, Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
