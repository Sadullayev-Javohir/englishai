using Domain.Assessment;
using Domain.Common;
using Domain.Grammar;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Grammar;

public class GrammarLessonTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    private static GrammarExercise Ex(int correct = 0, string? hint = null) =>
        GrammarExercise.Create(
            GrammarExerciseType.FillInBlank, "I have ___ apple.", new[] { "a", "an", "the" }, correct, hint);

    private static GrammarLesson Curated(params GrammarExercise[] exercises) =>
        GrammarLesson.Curate(
            "Articles: a, an, the",
            ErrorCategory.Articles,
            CefrLevel.A2,
            "I bought a book. The book is good.",
            "grammar.articles.explanation",
            exercises.Length == 0 ? new[] { Ex() } : exercises,
            new[] { GrammarApplicationTask.Create(SkillType.Writing, "Write three sentences using a/an/the.") },
            Now);

    [Fact]
    public void Curate_produces_a_lesson_with_all_five_steps()
    {
        var lesson = Curated();

        lesson.Category.Should().Be(ErrorCategory.Articles);
        lesson.Level.Should().Be(CefrLevel.A2);
        lesson.ContextIntro.Should().NotBeEmpty();
        lesson.ExplanationCode.Should().Be("grammar.articles.explanation");
        lesson.Exercises.Should().ContainSingle();
        lesson.ApplicationTasks.Should().ContainSingle();
    }

    [Fact]
    public void Curate_rejects_empty_explanation_code()
    {
        var act = () => GrammarLesson.Curate(
            "Topic", ErrorCategory.Articles, CefrLevel.A2, "Intro text.", "   ",
            new[] { Ex() }, Array.Empty<GrammarApplicationTask>(), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Curate_rejects_a_lesson_with_no_exercises()
    {
        var act = () => GrammarLesson.Curate(
            "Topic", ErrorCategory.Articles, CefrLevel.A2, "Intro text.", "code",
            Array.Empty<GrammarExercise>(), Array.Empty<GrammarApplicationTask>(), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GradeExercises_counts_unanswered_as_wrong()
    {
        var e1 = Ex(1);
        var e2 = Ex(2);
        var lesson = Curated(e1, e2);

        var result = lesson.GradeExercises(new Dictionary<Guid, int> { [e1.Id] = 1 });

        result.TotalExercises.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        result.Outcomes.Single(o => o.ExerciseId == e2.Id).IsCorrect.Should().BeFalse();
        result.Outcomes.Single(o => o.ExerciseId == e2.Id).SelectedOptionIndex.Should().Be(-1);
    }

    [Fact]
    public void GradeExercises_passes_at_or_above_seventy_percent()
    {
        var exercises = Enumerable.Range(0, 10).Select(_ => Ex(0)).ToArray();
        var lesson = Curated(exercises);

        var sevenCorrect = lesson.GradeExercises(exercises
            .Select((exercise, index) => new KeyValuePair<Guid, int>(exercise.Id, index < 7 ? 0 : 1))
            .ToDictionary());
        sevenCorrect.ScorePercent.Should().Be(70);
        sevenCorrect.Passed.Should().BeTrue();

        var sixCorrect = lesson.GradeExercises(exercises
            .Select((exercise, index) => new KeyValuePair<Guid, int>(exercise.Id, index < 6 ? 0 : 1))
            .ToDictionary());
        sixCorrect.ScorePercent.Should().Be(60);
        sixCorrect.Passed.Should().BeFalse();
    }

    [Fact]
    public void ForTopic_creates_a_pending_topic_scoped_shell()
    {
        var topicId = Guid.NewGuid();

        var lesson = GrammarLesson.ForTopic(
            topicId, "The Museum", ErrorCategory.Articles, CefrLevel.B1, "articles", Now);

        lesson.VocabularyTopicId.Should().Be(topicId);
        lesson.GrammarFocusCode.Should().Be("articles");
        lesson.IsFilled.Should().BeFalse();
        lesson.Status.Should().Be(GrammarLessonStatus.Pending);
        lesson.Exercises.Should().BeEmpty();
    }

    [Fact]
    public void ForTopic_rejects_an_empty_topic_id_or_focus_code()
    {
        var bad = () => GrammarLesson.ForTopic(
            Guid.Empty, "The Museum", ErrorCategory.Articles, CefrLevel.B1, "articles", Now);
        bad.Should().Throw<DomainException>();

        var noFocus = () => GrammarLesson.ForTopic(
            Guid.NewGuid(), "The Museum", ErrorCategory.Articles, CefrLevel.B1, "  ", Now);
        noFocus.Should().Throw<DomainException>();
    }

    [Fact]
    public void FillContent_fills_the_five_steps_and_grades_with_english_explanations()
    {
        var lesson = GrammarLesson.ForTopic(
            Guid.NewGuid(), "The Museum", ErrorCategory.Articles, CefrLevel.B1, "articles", Now);
        var ex = GrammarExercise.Create(
            GrammarExerciseType.Recognition, "Pick the right one.", new[] { "a", "the" }, 1, null,
            "Because 'the' points at a known museum.");

        lesson.FillContent(
            "At the museum we use articles a lot.",
            "Use 'the' for a specific museum.",
            new[] { GrammarExample.Create("We visited the museum yesterday.") },
            new[] { GrammarCommonMistake.Create("Odatda 'a' o'rniga 'the' ishlatiladi.") },
            new[] { ex },
            new[] { GrammarApplicationTask.Create(SkillType.Speaking, "Describe the museum.") });

        lesson.IsFilled.Should().BeTrue();
        lesson.ContextIntro.Should().NotBeEmpty();
        lesson.Explanation.Should().Be("Use 'the' for a specific museum.");
        lesson.ApplicationTasks.Should().ContainSingle();

        var wrong = lesson.GradeExercises(new Dictionary<Guid, int> { [ex.Id] = 0 });
        wrong.Outcomes.Single().Explanation.Should().Be("Because 'the' points at a known museum.");
        wrong.Outcomes.Single().IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void FillContent_rejects_empty_context_or_explanation()
    {
        var lesson = GrammarLesson.ForTopic(
            Guid.NewGuid(), "The Museum", ErrorCategory.Articles, CefrLevel.B1, "articles", Now);

        var exOnly = new[] { Ex() };
        var noArgs = new[] { GrammarExample.Create("Example sentence.") };
        var noMistakes = new[] { GrammarCommonMistake.Create("Common mistake.") };
        var noTasks = new[] { GrammarApplicationTask.Create(SkillType.Writing, "Write a sentence.") };

        var noContext = () => lesson.FillContent("  ", "rule", noArgs, noMistakes, exOnly, noTasks);
        noContext.Should().Throw<DomainException>();

        var noRule = () => lesson.FillContent("intro", "  ", noArgs, noMistakes, exOnly, noTasks);
        noRule.Should().Throw<DomainException>();

        var noExercises = () => lesson.FillContent(
            "intro", "rule", noArgs, noMistakes, Array.Empty<GrammarExercise>(), noTasks);
        noExercises.Should().Throw<DomainException>();
    }
}
