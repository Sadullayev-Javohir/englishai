using Domain.Assessment;
using Domain.Common;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Assessment;

public class PlacementQuestionTests
{
    private static PlacementQuestion Build(
        IReadOnlyList<string>? options = null,
        int correctIndex = 0,
        string prompt = "Choose the correct article: ___ apple") =>
        PlacementQuestion.Create(
            Guid.NewGuid(),
            TestStage.Vocabulary,
            CefrLevel.A2,
            prompt,
            options ?? new[] { "a", "an", "the", "(none)" },
            correctIndex);

    [Fact]
    public void Create_builds_a_valid_question()
    {
        var q = Build(correctIndex: 1);

        q.Options.Should().HaveCount(4);
        q.CorrectOptionIndex.Should().Be(1);
        q.Stage.Should().Be(TestStage.Vocabulary);
    }

    [Fact]
    public void IsCorrect_only_matches_the_correct_index()
    {
        var q = Build(correctIndex: 1);

        q.IsCorrect(1).Should().BeTrue();
        q.IsCorrect(0).Should().BeFalse();
    }

    [Fact]
    public void Create_rejects_empty_prompt()
    {
        var act = () => Build(prompt: "  ");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_fewer_than_two_options()
    {
        var act = () => Build(options: new[] { "only-one" });
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_out_of_range_correct_index()
    {
        var act = () => Build(correctIndex: 9);
        act.Should().Throw<DomainException>();
    }
}
