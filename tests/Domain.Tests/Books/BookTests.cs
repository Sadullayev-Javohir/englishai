using Domain.Assessment;
using Domain.Books;
using Domain.Common;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Books;

public class BookTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private static Book Curated(params string[] sections) =>
        Book.Curate(
            Guid.NewGuid(), "The Lost Key", "Yo'qolgan kalit", "EnglishAI", "A short mystery.",
            "mystery", CefrLevel.B1, "old key",
            sections.Length == 0 ? new[] { "One", "Two" } : sections, Now);

    private static IReadOnlyList<BookQuestion> TenQuestions(int correct = 0)
    {
        var list = new List<BookQuestion>();
        for (var i = 0; i < BookSection.QuestionsPerSection; i++)
            list.Add(BookQuestion.Create($"Question {i}?", new[] { "A", "B", "C" }, correct, "Because A."));
        return list;
    }

    [Fact]
    public void Curate_creates_pending_sections_in_order()
    {
        var book = Curated("First", "Second", "Third");

        book.SectionCount.Should().Be(3);
        book.Sections.Select(s => s.Order).Should().ContainInOrder(1, 2, 3);
        book.Sections.Should().OnlyContain(s => !s.IsFilled);
        book.HasCover.Should().BeFalse();
    }

    [Fact]
    public void Curate_rejects_a_book_with_no_sections()
    {
        var act = () => Book.Curate(
            Guid.NewGuid(), "T", "Tt", "A", "S", "topic", CefrLevel.A1, "cover",
            Array.Empty<string>(), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetCover_stores_url_and_attribution()
    {
        var book = Curated();

        book.SetCover("https://img/cover.jpg", "Photo by X on Unsplash");

        book.HasCover.Should().BeTrue();
        book.CoverImageUrl.Should().Be("https://img/cover.jpg");
        book.CoverAttribution.Should().Be("Photo by X on Unsplash");
    }

    [Fact]
    public void FillContent_requires_exactly_ten_questions()
    {
        var section = Curated().Sections[0];

        var tooFew = () => section.FillContent("Body text.", TenQuestions().Take(9));
        tooFew.Should().Throw<DomainException>();

        section.FillContent("Body text.", TenQuestions());
        section.IsFilled.Should().BeTrue();
        section.Questions.Should().HaveCount(10);
        section.WordCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GradeQuiz_passes_at_seven_of_ten_and_counts_unanswered_as_wrong()
    {
        var book = Curated("Only");
        var section = book.Sections[0];
        var questions = TenQuestions(correct: 1).ToList();
        section.FillContent("Body text.", questions);

        // Answer 7 correctly (index 1), leave the rest unanswered.
        var answers = questions.Take(7).ToDictionary(q => q.Id, _ => 1);
        var result = section.GradeQuiz(answers);

        result.TotalQuestions.Should().Be(10);
        result.CorrectCount.Should().Be(7);
        result.Passed.Should().BeTrue();
        result.ScorePercent.Should().Be(70);
    }

    [Fact]
    public void Quiz_result_requires_at_least_seventy_percent_for_any_question_count()
    {
        var sixOfNine = new BookQuizResult(Guid.NewGuid(), 9, 6, Array.Empty<BookQuestionOutcome>());
        var sevenOfNine = new BookQuizResult(Guid.NewGuid(), 9, 7, Array.Empty<BookQuestionOutcome>());

        sixOfNine.RequiredCorrect.Should().Be(7);
        sixOfNine.Passed.Should().BeFalse();
        sevenOfNine.Passed.Should().BeTrue();
    }

    [Fact]
    public void GradeQuiz_fails_below_seven_correct()
    {
        var book = Curated("Only");
        var section = book.Sections[0];
        var questions = TenQuestions(correct: 1).ToList();
        section.FillContent("Body text.", questions);

        var answers = questions.Take(6).ToDictionary(q => q.Id, _ => 1);
        var result = section.GradeQuiz(answers);

        result.CorrectCount.Should().Be(6);
        result.Passed.Should().BeFalse();
    }
}
