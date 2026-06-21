using Domain.Assessment;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Learning;

public class LearnerProfileTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private static PlacementResult Placement(CefrLevel overall, params (TestStage Stage, CefrLevel Level)[] stages)
    {
        var stageResults = stages.ToDictionary(
            s => s.Stage,
            s => new StageResult(s.Stage, s.Level, s.Level.ToScore()));
        return new PlacementResult(overall, overall.ToScore(), stageResults);
    }

    [Fact]
    public void CreateFromPlacement_seeds_every_skill()
    {
        var placement = Placement(
            CefrLevel.B1,
            (TestStage.Vocabulary, CefrLevel.B2),
            (TestStage.Grammar, CefrLevel.B1),
            (TestStage.Listening, CefrLevel.B1),
            (TestStage.Reading, CefrLevel.B1),
            (TestStage.Writing, CefrLevel.A2),
            (TestStage.Speaking, CefrLevel.A2));

        var profile = LearnerProfile.CreateFromPlacement(Learner, placement, Now);

        profile.Seeds.Should().HaveCount(6);
        profile.Seeds.Single(s => s.Skill == SkillType.Grammar).Score
            .Should().Be(CefrLevel.B1.ToScore());
        profile.Seeds.Single(s => s.Skill == SkillType.Vocabulary).Score
            .Should().Be(CefrLevel.B2.ToScore());
        profile.Seeds.Single(s => s.Skill == SkillType.Writing).Score
            .Should().Be(CefrLevel.A2.ToScore());
    }

    [Fact]
    public void Empty_learner_id_is_rejected()
    {
        var act = () => LearnerProfile.CreateFromPlacement(Guid.Empty, Placement(CefrLevel.A1), Now);

        act.Should().Throw<Domain.Common.DomainException>();
    }

    [Fact]
    public void Recorded_activity_overrides_the_seed_score()
    {
        var profile = LearnerProfile.CreateFromPlacement(
            Learner, Placement(CefrLevel.A2, (TestStage.Speaking, CefrLevel.A2)), Now);

        profile.RecordActivity(SkillType.Speaking, 95, Now);

        var speaking = profile.SkillScores(Now).Single(s => s.Skill == SkillType.Speaking);
        speaking.Score.Should().Be(95);
    }

    [Fact]
    public void Error_heatmap_counts_recent_errors_only()
    {
        var profile = LearnerProfile.CreateFromPlacement(Learner, Placement(CefrLevel.B1), Now);

        profile.RecordError(ErrorCategory.Articles, SkillType.Speaking, Now);
        profile.RecordError(ErrorCategory.Articles, SkillType.Writing, Now.AddDays(-2));
        profile.RecordError(ErrorCategory.Prepositions, SkillType.Speaking, Now);
        profile.RecordError(ErrorCategory.Modals, SkillType.Speaking, Now.AddDays(-60)); // outside window

        var heatmap = profile.ErrorHeatmap(Now);

        heatmap[ErrorCategory.Articles].Should().Be(2);
        heatmap[ErrorCategory.Prepositions].Should().Be(1);
        heatmap.Should().NotContainKey(ErrorCategory.Modals);
    }

    [Fact]
    public void TryAdvanceLevel_advances_only_when_eligible()
    {
        var profile = LearnerProfile.CreateFromPlacement(Learner, Placement(CefrLevel.B1), Now);

        // Master five skills with recent high activity.
        foreach (var skill in new[]
                 {
                     SkillType.Speaking, SkillType.Listening, SkillType.Reading,
                     SkillType.Writing, SkillType.Grammar
                 })
        {
            profile.RecordActivity(skill, 90, Now);
            profile.RecordActivity(skill, 90, Now.AddMinutes(1));
        }

        // Without the confirmation test, it must not advance.
        profile.TryAdvanceLevel(Now).Should().BeFalse();
        profile.OverallLevel.Should().Be(CefrLevel.B1);

        profile.RecordConfirmationTest(passed: true, Now);

        profile.TryAdvanceLevel(Now).Should().BeTrue();
        profile.OverallLevel.Should().Be(CefrLevel.B2);
        // Confirmation is consumed: the next level needs a fresh test.
        profile.LevelStatus(Now).ConfirmationTestPassed.Should().BeFalse();
    }

    [Fact]
    public void GrowthHistory_returns_a_point_per_skill_per_week()
    {
        var profile = LearnerProfile.CreateFromPlacement(Learner, Placement(CefrLevel.B1), Now);

        var history = profile.GrowthHistory(Now, weeks: 4);

        history.Should().HaveCount(4 * 6);
        history.Select(p => p.WeekEnding).Distinct().Should().HaveCount(4);
    }
}
