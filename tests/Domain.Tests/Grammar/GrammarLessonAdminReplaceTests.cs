using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Grammar;

public class GrammarLessonAdminReplaceTests
{
    [Fact]
    public void Admin_replace_updates_all_ordered_content()
    {
        var lesson = GrammarLesson.CreateManual("Old", ErrorCategory.Articles, CefrLevel.A1, DateTimeOffset.UtcNow);
        lesson.AdminReplace(
            "New", ErrorCategory.VerbTense, CefrLevel.B1, GrammarLessonStatus.Filled,
            null, "past-simple", "Context", "Rule",
            new[] { GrammarExample.Create("First"), GrammarExample.Create("Second") },
            new[] { GrammarCommonMistake.Create("Mistake") },
            new[] { GrammarExercise.Create(GrammarExerciseType.Recognition, "Question", new[] { "A", "B" }, 1, explanation: "Why") },
            new[] { GrammarApplicationTask.Create(SkillType.Writing, "Write it") });
        lesson.ReplaceCuratedRuleContent(
            "Qoida", "Mazmun", new[] { "Formula" },
            new[] { GrammarCuratedRule.Create("Tuzilishi", "Izoh") });

        lesson.Topic.Should().Be("New");
        lesson.Status.Should().Be(GrammarLessonStatus.Filled);
        lesson.Examples.Select(x => x.English).Should().Equal("First", "Second");
        lesson.Exercises[0].CorrectOptionIndex.Should().Be(1);
        lesson.ApplicationTasks.Should().ContainSingle();
        lesson.CuratedRules.Should().ContainSingle();
    }
}
