using Domain.Assessment;
using Domain.Learning;
using Domain.Writing;
using FluentAssertions;
using Infrastructure.Writing;
using Xunit;

namespace Integration.Tests.Writing;

/// <summary>
/// Direct unit coverage of the deterministic <see cref="LocalWritingAssessor"/> and the
/// vetted Uzbek content provider (no HTTP, no LLM, no Docker).
/// </summary>
public class WritingInfrastructureTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    private static WritingTask Task() =>
        WritingTask.Create("Write about your day.", CefrLevel.A2, 50, 80, Now);

    [Fact]
    public async Task Local_assessor_flags_a_subject_verb_slip_as_a_grammar_heatmap_issue()
    {
        var assessor = new LocalWritingAssessor();

        var assessment = await assessor.AssessAsync(
            Task(), "He go to the market. She do the work.", CefrLevel.A2, default);

        assessment.DimensionScores.Should().HaveCount(4);
        assessment.Issues.Should().Contain(i =>
            i.Dimension == WritingDimension.GrammaticalAccuracy &&
            i.Category == ErrorCategory.SubjectVerbAgreement);

        // The grammar score drops below the maximum when slips are detected.
        var grammar = assessment.DimensionScores.Single(d => d.Dimension == WritingDimension.GrammaticalAccuracy);
        grammar.Score.Should().BeLessThan(WritingAssessment.MaxDimensionScore);
    }

    [Fact]
    public async Task Local_assessor_rewards_a_well_formed_in_range_submission()
    {
        var assessor = new LocalWritingAssessor();
        var text = string.Join(' ', Enumerable.Range(0, 60).Select(i => $"word{i}")) +
                   ". However, this is a clear and varied paragraph. Therefore the score is high.";

        var assessment = await assessor.AssessAsync(Task(), text, CefrLevel.A2, default);

        assessment.Issues.Should().NotContain(i => i.Dimension == WritingDimension.GrammaticalAccuracy);
        assessment.OverallBand.Should().BeGreaterThan(3.0);
    }

    [Theory]
    [InlineData("asdf qwer zxcv hjkl lkjh poiu mnbv ghjk tyui rewq")] // random keystrokes
    [InlineData("asdfghjkl qwertyuiop zxcvbnm wertyu sdfghj")]          // keyboard rows
    [InlineData(" catta kichik shahar men yaxshi ko'raman bugun")]      // not English (uz)
    [InlineData("123 456 !!! ??? ... @@@")]                             // no words at all
    public async Task Local_assessor_floors_non_english_gibberish(string gibberish)
    {
        var assessor = new LocalWritingAssessor();

        var assessment = await assessor.AssessAsync(Task(), gibberish, CefrLevel.A2, default);

        // Random keystrokes used to earn top lexical/grammar marks (every token is "unique"
        // and no slip is detectable) and inflate the placement level. Now every dimension is
        // floored and the result carries the explanatory code - so the level lands at A1.
        assessment.DimensionScores.Should().OnlyContain(d => d.Score == WritingAssessment.MinDimensionScore);
        assessment.OverallPercent.Should().Be(0);
        assessment.EstimatedLevel.Should().Be(CefrLevel.A1);
        assessment.Issues.Should().ContainSingle(i => i.IssueCode == "writing.issue.not_english");
    }

    [Fact]
    public async Task Local_assessor_accepts_a_beginners_real_but_imperfect_answer()
    {
        var assessor = new LocalWritingAssessor();

        // Misspellings and topic words ("citi", "famaly") are not in the recognized set, but
        // the everyday words keep the ratio above the floor - a real learner is not punished.
        var assessment = await assessor.AssessAsync(
            Task(), "I like my citi. It is big and I have a big famaly here.", CefrLevel.A2, default);

        assessment.Issues.Should().NotContain(i => i.IssueCode == "writing.issue.not_english");
    }

    [Fact]
    public async Task Local_assessor_awards_full_marks_when_clear_simple_writing_meets_a1_expectations()
    {
        var assessor = new LocalWritingAssessor();
        const string text =
            "My name is Ali. I live in Tashkent. I like my family and my school. " +
            "I go to school every day. I read a book at home. I am happy today. " +
            "My friend is at school with me. We play and talk after school.";

        var assessment = await assessor.AssessAsync(Task(), text, CefrLevel.A1, default);

        assessment.DimensionScores.Should().OnlyContain(score => score.Score == 5);
        assessment.OverallBand.Should().Be(5);
        assessment.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Content_provider_resolves_vetted_uzbek_issue_explanations()
    {
        var provider = JsonWritingContentProvider.FromEmbeddedResource();

        provider.GetIssueExplanation("writing.issue.subject_verb_agreement")
            .Should().NotBeNullOrWhiteSpace();
        provider.GetIssueExplanation("writing.issue.does_not_exist").Should().BeNull();
    }
}
