using Domain.Common;
using Domain.Speaking;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Speaking;

public class PronunciationResultTests
{
    private static WordPronunciation Word(string w, double score, PronunciationErrorType err = PronunciationErrorType.None) =>
        new(w, score, err);

    [Fact]
    public void Score_at_or_above_80_is_Good()
    {
        var result = new PronunciationResult(82, 82, 80, 100, new[] { Word("hello", 82) });
        result.Band.Should().Be(PronunciationBand.Good);
    }

    [Fact]
    public void Score_below_80_needs_improvement()
    {
        var result = new PronunciationResult(74, 74, 70, 90, new[] { Word("three", 60) });
        result.Band.Should().Be(PronunciationBand.NeedsImprovement);
    }

    [Fact]
    public void Words_needing_practice_are_returned_weakest_first()
    {
        var result = new PronunciationResult(70, 70, 70, 100, new[]
        {
            Word("the", 55),
            Word("good", 95),
            Word("three", 40, PronunciationErrorType.Mispronunciation)
        });

        result.WordsNeedingPractice.Select(w => w.Word)
            .Should().Equal("three", "the");
    }

    [Fact]
    public void High_accuracy_clears_a_contradictory_mispronunciation_error()
    {
        var word = Word("water", 92, PronunciationErrorType.Mispronunciation);
        word.ErrorType.Should().Be(PronunciationErrorType.None);
        word.NeedsPractice.Should().BeFalse();
    }

    [Theory]
    [InlineData(PronunciationErrorType.Omission)]
    [InlineData(PronunciationErrorType.Insertion)]
    public void Structural_errors_still_need_practice_with_high_accuracy(PronunciationErrorType errorType)
    {
        var word = Word("water", 100, errorType);
        word.ErrorType.Should().Be(errorType);
        word.NeedsPractice.Should().BeTrue();
    }

    [Fact]
    public void Weakest_phoneme_is_the_lowest_scoring()
    {
        var word = new WordPronunciation("three", 60, PronunciationErrorType.Mispronunciation, new[]
        {
            new PhonemePronunciation("θ", 30),
            new PhonemePronunciation("r", 70),
            new PhonemePronunciation("iː", 80)
        });

        word.WeakestPhoneme!.Phoneme.Should().Be("θ");
    }

    [Fact]
    public void Overall_score_out_of_range_throws()
    {
        var act = () => new PronunciationResult(120, 100, 100, 100, new[] { Word("hi", 100) });
        act.Should().Throw<DomainException>();
    }
}
