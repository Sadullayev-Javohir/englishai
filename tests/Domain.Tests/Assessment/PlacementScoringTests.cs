using Domain.Assessment;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Assessment;

public class PlacementScoringTests
{
    private static AnswerRecord Answer(TestStage stage, CefrLevel difficulty, bool correct) =>
        new(Guid.NewGuid(), stage, difficulty, correct);

    private static IEnumerable<AnswerRecord> Many(TestStage stage, CefrLevel difficulty, bool correct, int count) =>
        Enumerable.Range(0, count).Select(_ => Answer(stage, difficulty, correct));

    public static IEnumerable<object[]> ReceptiveStages =>
        new[] { TestStage.Vocabulary, TestStage.Grammar, TestStage.Listening, TestStage.Reading }
            .Select(stage => new object[] { stage });

    [Fact]
    public void Empty_answers_default_to_A1_with_zero_score()
    {
        var result = PlacementScoring.Calculate(Array.Empty<AnswerRecord>());

        result.OverallLevel.Should().Be(CefrLevel.A1);
        result.OverallScore.Should().Be(0);
        result.StageResults.Should().BeEmpty();
    }

    [Fact]
    public void Consistent_success_at_a_level_places_the_learner_at_that_level()
    {
        // Correct on everything up to B1, wrong above it -> the estimate settles at B1.
        var answers = new List<AnswerRecord>();
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.A2, correct: true, 3));
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.B1, correct: true, 4));
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.B2, correct: false, 3));

        var result = PlacementScoring.Calculate(answers);

        result.StageResults[TestStage.Vocabulary].Level.Should().Be(CefrLevel.B1);
    }

    [Fact]
    public void A_single_lucky_guess_on_a_hard_item_does_not_inflate_the_level()
    {
        // The core regression: an A2-ish learner gets the easy items right, fails the
        // harder ones, but flukes ONE C1 item. The old "highest correct" rule would
        // have rated this C1; the guessing-aware estimate must stay low (A2/B1).
        var answers = new List<AnswerRecord>();
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.A2, correct: true, 6));
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.B1, correct: false, 4));
        answers.Add(Answer(TestStage.Vocabulary, CefrLevel.C1, correct: true)); // fluke

        var result = PlacementScoring.Calculate(answers);

        result.StageResults[TestStage.Vocabulary].Level
            .Should().BeOneOf(CefrLevel.A2, CefrLevel.B1);
        result.OverallLevel.Should().BeOneOf(CefrLevel.A2, CefrLevel.B1);
    }

    [Fact]
    public void Mostly_wrong_answers_floor_the_level_near_A1()
    {
        var answers = new List<AnswerRecord>();
        answers.AddRange(Many(TestStage.Reading, CefrLevel.A2, correct: false, 4));
        answers.AddRange(Many(TestStage.Reading, CefrLevel.B1, correct: false, 2));

        var result = PlacementScoring.Calculate(answers);

        result.StageResults[TestStage.Reading].Level.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void Strong_consistent_performance_reaches_the_upper_levels()
    {
        var answers = new List<AnswerRecord>();
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.B2, correct: true, 4));
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.C1, correct: true, 4));
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.C2, correct: false, 2));

        var result = PlacementScoring.Calculate(answers);

        result.StageResults[TestStage.Vocabulary].Level
            .Should().BeOneOf(CefrLevel.B2, CefrLevel.C1);
    }

    [Fact]
    public void Weak_speaking_pulls_the_overall_score_down_via_its_weight()
    {
        // Strong on the reading/listening/vocab side, but consistently failing speaking
        // keeps the overall placement below the top of those stages.
        var answers = new List<AnswerRecord>();
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.B2, correct: true, 4));
        answers.AddRange(Many(TestStage.Vocabulary, CefrLevel.C1, correct: false, 2));
        answers.AddRange(Many(TestStage.Speaking, CefrLevel.A2, correct: false, 3));

        var result = PlacementScoring.Calculate(answers);

        result.StageResults[TestStage.Speaking].Level.Should().Be(CefrLevel.A1);
        result.OverallLevel.Should().BeOneOf(CefrLevel.A2, CefrLevel.B1);
    }

    [Theory]
    [MemberData(nameof(ReceptiveStages))]
    public void Mostly_wrong_receptive_answers_remain_at_A1(TestStage stage)
    {
        var answers = new List<AnswerRecord>();
        answers.AddRange(Many(stage, CefrLevel.A2, correct: false, 4));
        answers.AddRange(Many(stage, CefrLevel.B1, correct: false, 2));

        var result = PlacementScoring.Calculate(answers);

        result.StageResults[stage].Level.Should().Be(CefrLevel.A1);
    }

    [Theory]
    [MemberData(nameof(ReceptiveStages))]
    public void One_lucky_advanced_answer_does_not_inflate_any_receptive_skill(TestStage stage)
    {
        var answers = new List<AnswerRecord>();
        answers.AddRange(Many(stage, CefrLevel.A2, correct: true, 4));
        answers.AddRange(Many(stage, CefrLevel.B1, correct: false, 3));
        answers.Add(Answer(stage, CefrLevel.C1, correct: true));

        var result = PlacementScoring.Calculate(answers);

        result.StageResults[stage].Level.Should().BeOneOf(CefrLevel.A1, CefrLevel.A2, CefrLevel.B1);
    }

    [Theory]
    [InlineData(CefrLevel.A1, 100, CefrLevel.A2)]
    [InlineData(CefrLevel.A2, 100, CefrLevel.B1)]
    [InlineData(CefrLevel.B1, 100, CefrLevel.B2)]
    [InlineData(CefrLevel.B2, 100, CefrLevel.C1)]
    public void Productive_score_cannot_exceed_one_band_above_task(
        CefrLevel taskDifficulty, int rawScore, CefrLevel expectedMaximum)
    {
        var productive = new[]
        {
            new ProductiveResult(TestStage.Speaking, rawScore, taskDifficulty),
        };

        var result = PlacementScoring.Calculate(Array.Empty<AnswerRecord>(), productive);

        result.StageResults[TestStage.Speaking].Level.Should().Be(expectedMaximum);
    }

    [Fact]
    public void Six_skill_result_preserves_each_stage_and_uses_balanced_overall_score()
    {
        var answers = new List<AnswerRecord>();
        foreach (var stage in new[] { TestStage.Vocabulary, TestStage.Grammar, TestStage.Listening, TestStage.Reading })
        {
            answers.AddRange(Many(stage, CefrLevel.A2, correct: true, 4));
            answers.AddRange(Many(stage, CefrLevel.B1, correct: false, 2));
        }

        var productive = new[]
        {
            new ProductiveResult(TestStage.Writing, 35, CefrLevel.A1),
            new ProductiveResult(TestStage.Speaking, 35, CefrLevel.A1),
        };

        var result = PlacementScoring.Calculate(answers, productive);

        result.StageResults.Should().HaveCount(6);
        result.StageResults[TestStage.Writing].Level.Should().Be(CefrLevel.A2);
        result.StageResults[TestStage.Speaking].Level.Should().Be(CefrLevel.A2);
        result.OverallLevel.Should().BeOneOf(CefrLevel.A1, CefrLevel.A2, CefrLevel.B1);
    }
}
