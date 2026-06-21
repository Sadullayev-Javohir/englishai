using Domain.Assessment;
using Domain.Common;
using Domain.Reading;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Reading;

public class ReadingPassageTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    private static ReadingQuestion Q(int correct = 0, string? hint = null) =>
        ReadingQuestion.Create("What is the main idea?", new[] { "A", "B", "C" }, correct, hint);

    private static ReadingPassage Curated(params ReadingQuestion[] questions) =>
        ReadingPassage.Curate(
            "The Power of Habits",
            "Small habits compound into remarkable results over time.",
            "psychology",
            CefrLevel.B2,
            new[] { GlossaryEntry.Create("compound", "to'planib o'sib bormoq") },
            questions.Length == 0 ? new[] { Q() } : questions,
            Now);

    [Fact]
    public void Curate_produces_a_passage_ready_to_show()
    {
        var passage = Curated();

        passage.Level.Should().Be(CefrLevel.B2);
        passage.Glossary.Should().ContainSingle();
        passage.Questions.Should().ContainSingle();
        passage.WordCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Curate_rejects_empty_body()
    {
        var act = () => ReadingPassage.Curate(
            "Title", "   ", "topic", CefrLevel.A2,
            Array.Empty<GlossaryEntry>(), new[] { Q() }, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GradeQuiz_counts_unanswered_questions_as_wrong()
    {
        var q1 = Q(1);
        var q2 = Q(2);
        var passage = Curated(q1, q2);

        var result = passage.GradeQuiz(new Dictionary<Guid, int> { [q1.Id] = 1 });

        result.TotalQuestions.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        result.Outcomes.Single(o => o.QuestionId == q2.Id).IsCorrect.Should().BeFalse();
        result.Outcomes.Single(o => o.QuestionId == q2.Id).SelectedOptionIndex.Should().Be(-1);
    }

    [Fact]
    public void GradeQuiz_requires_at_least_seventy_five_percent()
    {
        var questions = Enumerable.Range(0, 100).Select(_ => Q(0)).ToArray();
        var passage = Curated(questions);

        var below = questions
            .Select((question, index) => new KeyValuePair<Guid, int>(question.Id, index < 74 ? 0 : 1))
            .ToDictionary();
        var boundary = questions
            .Select((question, index) => new KeyValuePair<Guid, int>(question.Id, index < 75 ? 0 : 1))
            .ToDictionary();

        var failed = passage.GradeQuiz(below);
        var passed = passage.GradeQuiz(boundary);

        failed.ScorePercent.Should().Be(74);
        failed.Passed.Should().BeFalse();
        passed.ScorePercent.Should().Be(75);
        passed.Passed.Should().BeTrue();
    }

    [Fact]
    public void GradeQuiz_throws_when_no_questions()
    {
        var passage = ReadingPassage.Curate(
            "Title", "Body text here.", "topic", CefrLevel.A2,
            Array.Empty<GlossaryEntry>(), Array.Empty<ReadingQuestion>(), Now);

        var act = () => passage.GradeQuiz(new Dictionary<Guid, int>());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AdminReplaceContent_replaces_the_full_aggregate_in_order()
    {
        var passage = Curated();
        var vocabulary = new[]
        {
            GlossaryEntry.Create("garden", "bog'", "A city garden."),
            GlossaryEntry.Create("resident", "yashovchi"),
        };
        var questions = new[]
        {
            ReadingQuestion.Create("Where is it?", new[] { "City", "Village" }, 0, explanation: "The passage says city."),
        };

        passage.AdminReplaceContent(
            "City gardens", "Nature", "environment", CefrLevel.B1,
            ReadingPassageStatus.Filled, "First paragraph.\n\nSecond paragraph.",
            "gap: garden", vocabulary, questions);

        passage.Title.Should().Be("City gardens");
        passage.Category.Should().Be("environment");
        passage.ContextGaps.Should().Be("gap: garden");
        passage.Glossary.Select(entry => entry.Word).Should().ContainInOrder("garden", "resident");
        passage.Questions.Should().ContainSingle().Which.CorrectOptionIndex.Should().Be(0);
    }
}
