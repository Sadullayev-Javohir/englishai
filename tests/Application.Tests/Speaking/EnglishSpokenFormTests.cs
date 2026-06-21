using Application.Speaking.Common;
using Domain.Speaking;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Speaking;

public class EnglishSpokenFormTests
{
    [Theory]
    [InlineData("5:00", "five o'clock")]
    [InlineData("5:00 AM", "five AM")]
    [InlineData("7:30 PM", "seven thirty PM")]
    [InlineData("21", "twenty-one")]
    [InlineData("3.5", "three point five")]
    [InlineData("2026", "twenty twenty-six")]
    public void Converts_numeric_tokens_to_english_spoken_forms(string input, string expected)
    {
        EnglishSpokenForm.ToSpoken(input).Should().Be(expected);
    }

    [Fact]
    public void Normalizes_sentence_and_maps_assessment_words_back_to_original_token()
    {
        var reference = EnglishSpokenForm.NormalizeText("I wake up at 5:00 AM.");
        var assessment = new PronunciationResult(
            72, 72, 75, 100,
            new[]
            {
                Word("I", 95), Word("wake", 94), Word("up", 93), Word("at", 92),
                Word("five", 55), Word("AM", 90),
            });

        var mapped = reference.MapResult(assessment);

        reference.Spoken.Should().Be("I wake up at five AM.");
        mapped.Words.Should().ContainSingle(word =>
            word.Word == "5:00 AM" && word.SpokenForm == "five AM" && word.AccuracyScore == 55);
        mapped.Words.Should().NotContain(word => word.Word == "five");
    }

    private static WordPronunciation Word(string word, double score) =>
        new(word, score, score < 80 ? PronunciationErrorType.Mispronunciation : PronunciationErrorType.None);
}
