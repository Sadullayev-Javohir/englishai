using Domain.Grammar;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Grammar;

public class GrammarTypedAnswerTests
{
    [Theory]
    [InlineData("finished", 0)]
    [InlineData("  FINISHED. ", 0)]
    [InlineData("finish", 1)]
    [InlineData("finisht", -1)]
    [InlineData("", -1)]
    [InlineData("   ", -1)]
    public void Typed_blank_matches_only_a_canonical_form(string answer, int expected)
    {
        var exercise = GrammarExercise.Create(GrammarExerciseType.FillInBlank,
            "I have ___ my work.", new[] { "finished", "finish" }, 0);
        exercise.MatchTextAnswer(answer).Should().Be(expected);
    }

    [Theory]
    [InlineData("finished", 0)]
    [InlineData("I have finished my work.", 0)]
    [InlineData("finish", 1)]
    [InlineData("will finish", -1)]
    public void Typed_blank_can_match_options_stored_as_complete_sentences(string answer, int expected)
    {
        var exercise = GrammarExercise.Create(GrammarExerciseType.FillInBlank,
            "I have ___ my work.", new[] { "I have finished my work.", "I have finish my work." }, 0);
        exercise.MatchTextAnswer(answer).Should().Be(expected);
    }

    [Theory]
    [InlineData(" i HAVE   finished my work! ", 0)]
    [InlineData("I finish my work.", 1)]
    [InlineData("finished", -1)]
    [InlineData("I have not finished my work.", -1)]
    public void Rephrase_requires_the_full_sentence_and_preserves_grammar(string answer, int expected)
    {
        var exercise = GrammarExercise.Create(GrammarExerciseType.Rephrase,
            "I finished my work. I am free now.", new[] { "I have finished my work.", "I finish my work." }, 0);
        exercise.MatchTextAnswer(answer).Should().Be(expected);
    }
}
