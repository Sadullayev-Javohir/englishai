using Domain.Speaking;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Speaking;

public class SpeakingPracticeWordTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Word_is_mastered_only_after_three_successes_and_failure_reactivates_it()
    {
        var word = SpeakingPracticeWord.Create(
            Guid.NewGuid(), "Hello!", 54, PronunciationErrorType.Mispronunciation, Now);

        word.RecordPractice(85, Now.AddMinutes(1));
        word.SuccessfulAttemptCount.Should().Be(1);
        word.IsActive.Should().BeTrue();

        word.RecordPractice(90, Now.AddMinutes(2));
        word.SuccessfulAttemptCount.Should().Be(2);
        word.IsActive.Should().BeTrue();

        word.RecordPractice(88, Now.AddMinutes(3));
        word.SuccessfulAttemptCount.Should().Be(3);
        word.IsActive.Should().BeFalse();

        word.RecordFailure("HELLO", 71, PronunciationErrorType.Mispronunciation, Now.AddDays(1));

        word.IsActive.Should().BeTrue();
        word.SuccessfulAttemptCount.Should().Be(0);
        word.ErrorCount.Should().Be(2);
        word.NormalizedWord.Should().Be("hello");
    }

    [Fact]
    public void Practice_below_85_keeps_word_active_and_does_not_count_as_a_success()
    {
        var word = SpeakingPracticeWord.Create(
            Guid.NewGuid(), "world", 60, PronunciationErrorType.Mispronunciation, Now);

        word.RecordPractice(84.9, Now.AddMinutes(1));

        word.IsActive.Should().BeTrue();
        word.SuccessfulAttemptCount.Should().Be(0);
        word.BestPracticeScore.Should().Be(84.9);
    }
}
