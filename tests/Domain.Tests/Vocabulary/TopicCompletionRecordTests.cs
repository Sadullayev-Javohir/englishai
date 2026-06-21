using Domain.Assessment;
using Domain.Common;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Vocabulary;

public class TopicCompletionRecordTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

    private static TopicCompletionRecord NewRecord() =>
        TopicCompletionRecord.Start(Guid.NewGuid(), Guid.NewGuid(), CefrLevel.A2, Now);

    [Fact]
    public void Start_initializes_an_unmastered_record()
    {
        var record = NewRecord();

        record.IsMastered.Should().BeFalse();
        record.MasteredAt.Should().BeNull();
        record.PassedModuleCount.Should().Be(0);
        record.ModuleScores.Should().BeEmpty();
        record.Level.Should().Be(CefrLevel.A2);
    }

    [Fact]
    public void Start_rejects_empty_ids()
    {
        var act1 = () => TopicCompletionRecord.Start(Guid.Empty, Guid.NewGuid(), CefrLevel.A2, Now);
        var act2 = () => TopicCompletionRecord.Start(Guid.NewGuid(), Guid.Empty, CefrLevel.A2, Now);

        act1.Should().Throw<DomainException>();
        act2.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RecordModule_rejects_out_of_range_scores(int score)
    {
        var record = NewRecord();

        var act = () => record.RecordModule(SkillType.Vocabulary, score, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordModule_below_threshold_does_not_pass_the_module()
    {
        var record = NewRecord();

        record.RecordModule(SkillType.Vocabulary, 74, Now);

        record.IsModulePassed(SkillType.Vocabulary).Should().BeFalse();
        record.PassedModuleCount.Should().Be(0);
        record.ScoreFor(SkillType.Vocabulary).Should().Be(74);
    }

    [Fact]
    public void RecordModule_at_threshold_passes_the_module()
    {
        var record = NewRecord();

        record.RecordModule(SkillType.Vocabulary, 75, Now);

        record.IsModulePassed(SkillType.Vocabulary).Should().BeTrue();
        record.PassedModuleCount.Should().Be(1);
    }

    [Fact]
    public void RecordModule_uses_seventy_percent_threshold_for_grammar_only()
    {
        var record = NewRecord();

        record.RecordModule(SkillType.Grammar, 70, Now);
        record.RecordModule(SkillType.Vocabulary, 70, Now);

        record.IsModulePassed(SkillType.Grammar).Should().BeTrue();
        record.IsModulePassed(SkillType.Vocabulary).Should().BeFalse();
        record.PassedModuleCount.Should().Be(1);
    }

    [Fact]
    public void RecordModule_keeps_the_best_score_and_ignores_a_lower_later_attempt()
    {
        var record = NewRecord();

        record.RecordModule(SkillType.Reading, 85, Now);
        record.RecordModule(SkillType.Reading, 60, Now.AddDays(1));

        record.ScoreFor(SkillType.Reading).Should().Be(85);
        record.IsModulePassed(SkillType.Reading).Should().BeTrue();
        record.ModuleScores.Should().ContainSingle(m => m.Module == SkillType.Reading);
    }

    [Fact]
    public void RecordModule_raises_the_score_when_a_later_attempt_is_higher()
    {
        var record = NewRecord();
        var later = Now.AddDays(2);

        record.RecordModule(SkillType.Grammar, 50, Now);
        record.RecordModule(SkillType.Grammar, 90, later);

        record.ScoreFor(SkillType.Grammar).Should().Be(90);
        record.ModuleScores.Single(m => m.Module == SkillType.Grammar).AchievedAt.Should().Be(later);
    }

    [Fact]
    public void Topic_becomes_mastered_only_when_all_six_modules_pass()
    {
        var record = NewRecord();
        var modules = TopicCompletionRecord.RequiredModules;

        modules.Should().ContainInOrder(
            SkillType.Vocabulary,
            SkillType.Grammar,
            SkillType.Reading,
            SkillType.Writing,
            SkillType.Speaking,
            SkillType.Listening);

        // Pass the first five - still not mastered.
        foreach (var module in modules.Take(5))
        {
            var transitioned = record.RecordModule(module, 75, Now);
            transitioned.Should().BeFalse();
        }

        record.IsMastered.Should().BeFalse();
        record.PassedModuleCount.Should().Be(5);

        // The sixth tips it over and reports the transition exactly once.
        var mastered = record.RecordModule(modules[5], 75, Now);

        mastered.Should().BeTrue();
        record.IsMastered.Should().BeTrue();
        record.MasteredAt.Should().Be(Now);
        record.PassedModuleCount.Should().Be(6);
    }

    [Fact]
    public void All_modules_are_unlocked_before_any_progress()
    {
        var record = NewRecord();
        var modules = TopicCompletionRecord.RequiredModules;

        modules.Should().OnlyContain(module => record.IsModuleUnlocked(module));
        record.RecommendedNextModule.Should().Be(SkillType.Vocabulary);
    }

    [Fact]
    public void Recommendation_advances_without_locking_other_modules()
    {
        var record = NewRecord();
        var modules = TopicCompletionRecord.RequiredModules;

        record.RecordModule(modules[0], 75, Now);
        record.RecommendedNextModule.Should().Be(modules[1]);
        modules.Should().OnlyContain(module => record.IsModuleUnlocked(module));

        record.RecordModule(modules[1], 69, Now);
        record.RecommendedNextModule.Should().Be(modules[1]);
        modules.Should().OnlyContain(module => record.IsModuleUnlocked(module));
    }

    [Fact]
    public void ResetModule_removes_score_and_revokes_mastery()
    {
        var record = NewRecord();
        foreach (var module in TopicCompletionRecord.RequiredModules)
            record.RecordModule(module, 80, Now);

        record.ResetModule(SkillType.Reading, Now.AddDays(1));

        record.ScoreFor(SkillType.Reading).Should().Be(0);
        record.ModuleScores.Should().NotContain(score => score.Module == SkillType.Reading);
        record.IsMastered.Should().BeFalse();
        record.MasteredAt.Should().BeNull();
        record.IsModuleUnlocked(SkillType.Writing).Should().BeTrue();
    }

    [Fact]
    public void Mastery_transition_is_reported_only_once()
    {
        var record = NewRecord();
        foreach (var module in TopicCompletionRecord.RequiredModules)
            record.RecordModule(module, 80, Now);

        var firstMasteredAt = record.MasteredAt;

        // A further passing result must not re-trigger the transition nor move MasteredAt.
        var transitioned = record.RecordModule(SkillType.Speaking, 95, Now.AddDays(3));

        transitioned.Should().BeFalse();
        record.MasteredAt.Should().Be(firstMasteredAt);
    }
}
