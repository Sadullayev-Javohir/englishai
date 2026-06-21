using Domain.Assessment;
using Domain.Common;
using Domain.Listening;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Listening;

public class ListeningExerciseTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static ListeningQuestion Q(int correct) =>
        ListeningQuestion.Create("What did the speaker say?", new[] { "A", "B", "C" }, correct, "lhint.x");

    private static ListeningExercise Exercise(params ListeningQuestion[] questions) =>
        ListeningExercise.Curate(
            "At the Café", "I would like a cup of tea, please.", "everyday", CefrLevel.A1,
            questions.Length == 0 ? new[] { Q(0) } : questions, Now);

    [Fact]
    public void Curate_requires_at_least_one_question()
    {
        var act = () => ListeningExercise.Curate(
            "Empty", "Some transcript.", "everyday", CefrLevel.A1,
            Array.Empty<ListeningQuestion>(), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Curate_rejects_blank_transcript()
    {
        var act = () => ListeningExercise.Curate(
            "Title", "   ", "everyday", CefrLevel.A1, new[] { Q(0) }, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void WordCount_counts_transcript_words()
    {
        var exercise = Exercise();

        // "I would like a cup of tea, please." → 8 words.
        exercise.WordCount.Should().Be(8);
    }

    [Fact]
    public void GradeQuiz_scores_correct_and_missing_answers()
    {
        var q1 = Q(1);
        var q2 = Q(2);
        var exercise = Exercise(q1, q2);

        // q1 answered correctly; q2 not submitted → counts as incorrect.
        var result = exercise.GradeQuiz(new Dictionary<Guid, int> { [q1.Id] = 1 });

        result.TotalQuestions.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        result.ScorePercent.Should().Be(50);
        result.Passed.Should().BeFalse();
        result.Outcomes.Single(o => o.QuestionId == q2.Id).SelectedOptionIndex.Should().Be(-1);
    }

    [Fact]
    public void GradeQuiz_passes_at_seventy_five_percent_threshold()
    {
        var questions = new[] { Q(0), Q(0), Q(0), Q(0) };
        var exercise = Exercise(questions);

        var result = exercise.GradeQuiz(new Dictionary<Guid, int>
        {
            [questions[0].Id] = 0,
            [questions[1].Id] = 0,
            [questions[2].Id] = 0,
            [questions[3].Id] = 1,
        });

        result.ScorePercent.Should().Be(75);
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public void GradeQuiz_fails_below_seventy_five_percent_threshold()
    {
        var questions = new[] { Q(0), Q(0), Q(0), Q(0) };
        var exercise = Exercise(questions);

        var result = exercise.GradeQuiz(new Dictionary<Guid, int>
        {
            [questions[0].Id] = 0,
            [questions[1].Id] = 0,
            [questions[2].Id] = 1,
            [questions[3].Id] = 1,
        });

        result.ScorePercent.Should().Be(50);
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public void AdminReplace_persists_segments_in_display_order()
    {
        var exercise = Exercise();

        exercise.AdminReplace(
            "Updated",
            "travel",
            CefrLevel.B1,
            ListeningExerciseStatus.Filled,
            null,
            "First sentence. Second sentence.",
            new[]
            {
                ListeningSegment.Create(2, 1000, 2000, "Speaker 2", "Second sentence."),
                ListeningSegment.Create(1, 0, 1000, "Speaker 1", "First sentence."),
            },
            new[] { Q(0) });

        exercise.Segments.Select(segment => segment.Order).Should().Equal(1, 2);
        exercise.Segments[0].Text.Should().Be("First sentence.");
    }
}
