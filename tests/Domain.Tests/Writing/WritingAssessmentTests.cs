using Domain.Assessment;
using Domain.Common;
using Domain.Learning;
using Domain.Writing;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Writing;

public class WritingAssessmentTests
{
    private static readonly Guid TaskId = Guid.NewGuid();

    private static DimensionScore[] Scores(int task, int coh, int lex, int gram) => new[]
    {
        new DimensionScore(WritingDimension.TaskAchievement, task),
        new DimensionScore(WritingDimension.Coherence, coh),
        new DimensionScore(WritingDimension.LexicalResource, lex),
        new DimensionScore(WritingDimension.GrammaticalAccuracy, gram),
    };

    [Fact]
    public void OverallBand_is_the_average_and_percent_projects_one_to_five_onto_zero_to_hundred()
    {
        var assessment = WritingAssessment.Create(TaskId, Scores(5, 5, 5, 5), Array.Empty<WritingIssue>());
        assessment.OverallBand.Should().Be(5.0);
        assessment.OverallPercent.Should().Be(100);
        assessment.EstimatedLevel.Should().Be(CefrLevel.C2);

        var floor = WritingAssessment.Create(TaskId, Scores(1, 1, 1, 1), Array.Empty<WritingIssue>());
        floor.OverallPercent.Should().Be(0);
        floor.EstimatedLevel.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void EstimatedLevel_maps_a_mid_band_to_an_intermediate_level()
    {
        var assessment = WritingAssessment.Create(TaskId, Scores(3, 3, 3, 3), Array.Empty<WritingIssue>());

        assessment.OverallBand.Should().Be(3.0);
        assessment.EstimatedLevel.Should().Be(CefrLevel.B1);
        assessment.OverallPercent.Should().Be(50);
    }

    [Fact]
    public void EstimateLevelCappedTo_limits_the_estimate_to_one_band_above_the_task_level()
    {
        // A perfect submission on an A1 task: the raw estimate is C2, but an A1 prompt never
        // demands C2-level production, so it is capped to A2 (task level + 1) - never C2.
        var perfect = WritingAssessment.Create(TaskId, Scores(5, 5, 5, 5), Array.Empty<WritingIssue>());
        perfect.EstimatedLevel.Should().Be(CefrLevel.C2);
        perfect.EstimateLevelCappedTo(CefrLevel.A1).Should().Be(CefrLevel.A2);
        perfect.EstimateLevelCappedTo(CefrLevel.B1).Should().Be(CefrLevel.B2);
    }

    [Fact]
    public void EstimateLevelCappedTo_does_not_raise_a_weak_estimate_and_never_exceeds_c2()
    {
        // The cap is a ceiling, not a floor: a weak A2-band submission stays A2 even on a C1 task.
        var weak = WritingAssessment.Create(TaskId, Scores(2, 2, 2, 2), Array.Empty<WritingIssue>());
        weak.EstimatedLevel.Should().Be(CefrLevel.A2);
        weak.EstimateLevelCappedTo(CefrLevel.C1).Should().Be(CefrLevel.A2);

        // A C2 task cannot be capped beyond C2.
        var perfect = WritingAssessment.Create(TaskId, Scores(5, 5, 5, 5), Array.Empty<WritingIssue>());
        perfect.EstimateLevelCappedTo(CefrLevel.C2).Should().Be(CefrLevel.C2);
    }

    [Fact]
    public void Create_requires_all_four_dimensions_exactly_once()
    {
        var missingGrammar = new[]
        {
            new DimensionScore(WritingDimension.TaskAchievement, 3),
            new DimensionScore(WritingDimension.Coherence, 3),
            new DimensionScore(WritingDimension.LexicalResource, 3),
        };

        var act = () => WritingAssessment.Create(TaskId, missingGrammar, Array.Empty<WritingIssue>());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_an_out_of_range_score()
    {
        var act = () => WritingAssessment.Create(TaskId, Scores(3, 3, 3, 6), Array.Empty<WritingIssue>());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_an_issue_with_an_empty_code()
    {
        var issues = new[]
        {
            new WritingIssue(WritingDimension.GrammaticalAccuracy, "   ", 0, 5, ErrorCategory.SubjectVerbAgreement),
        };

        var act = () => WritingAssessment.Create(TaskId, Scores(3, 3, 3, 3), issues);

        act.Should().Throw<DomainException>();
    }
}
