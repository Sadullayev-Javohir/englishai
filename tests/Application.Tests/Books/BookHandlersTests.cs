using Application.Books.CheckBookAnswer;
using Application.Books.Dtos;
using Application.Books.GetBookSection;
using Application.Books.GetBooksCatalog;
using Application.Books.Models;
using Application.Books.Ports;
using Application.Books.SubmitBookQuiz;
using Application.Common;
using Application.Gamification;
using Application.Learning.Ports;
using Application.Tests.Learning;
using Domain.Assessment;
using Domain.Books;
using Domain.Learning;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using NSubstitute;
using Xunit;

namespace Application.Tests.Books;

public class BookHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly IBookProgressStore _progress = Substitute.For<IBookProgressStore>();
    private readonly IBookContentGenerator _generator = Substitute.For<IBookContentGenerator>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IDailyProgressRecorder _dailyProgress = Substitute.For<IDailyProgressRecorder>();
    private readonly IImageService _images = Substitute.For<IImageService>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static Book BookWith(params string[] sections) =>
        Book.Curate(
            Guid.NewGuid(), "The Lost Key", "Yo'qolgan kalit", "EnglishAI", "A short mystery.",
            "mystery", CefrLevel.B1, "old key",
            sections.Length == 0 ? new[] { "One", "Two" } : sections, Now);

    private static GeneratedBookSection GeneratedSection()
    {
        var questions = new List<GeneratedBookQuestion>();
        for (var i = 0; i < BookSection.QuestionsPerSection; i++)
            questions.Add(new GeneratedBookQuestion($"Q{i}?", new[] { "A", "B", "C" }, 0, "Because A."));
        return new GeneratedBookSection("This is the section text.", questions);
    }

    private static LearnerProfile ProfileAt(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(), new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    [Fact]
    public async Task Catalog_resolves_and_persists_a_cover_image_once()
    {
        var book = BookWith();
        _books.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>()).Returns(new[] { book });
        _progress.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BookProgress>());
        _images.FindImageAsync("old key", Arg.Any<CancellationToken>())
            .Returns(new ImageResult("https://img/cover.jpg", "Photo by X on Unsplash"));
        var handler = new GetBooksCatalogQueryHandler(_books, _progress, _profiles, _images);

        var catalog = await handler.Handle(
            new GetBooksCatalogQuery(Learner, CefrLevel.B1, false), CancellationToken.None);

        catalog.Should().ContainSingle();
        catalog[0].CoverImageUrl.Should().Be("https://img/cover.jpg");
        catalog[0].SectionCount.Should().Be(2);
        catalog[0].SectionsRead.Should().Be(0);
        book.HasCover.Should().BeTrue();
        await _books.Received(1).SaveAsync(book, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Catalog_without_an_image_leaves_the_book_coverless()
    {
        var book = BookWith();
        _books.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>()).Returns(new[] { book });
        _progress.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<BookProgress>());
        _images.FindImageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((ImageResult?)null);
        var handler = new GetBooksCatalogQueryHandler(_books, _progress, _profiles, _images);

        var catalog = await handler.Handle(
            new GetBooksCatalogQuery(Learner, CefrLevel.B1, false), CancellationToken.None);

        catalog[0].CoverImageUrl.Should().BeNull();
        await _books.DidNotReceive().SaveAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSection_lazily_generates_and_caches_the_section()
    {
        var book = BookWith("One", "Two");
        var section = book.Sections[0];
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns((BookProgress?)null);
        _generator.GenerateAsync(
                book.Title, book.Synopsis, section.Title, section.Order, book.SectionCount, book.Level,
                Arg.Any<CancellationToken>())
            .Returns(GeneratedSection());
        var handler = new GetBookSectionQueryHandler(_books, _progress, _generator);

        var dto = await handler.Handle(
            new GetBookSectionQuery(book.Id, section.Id, Learner), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
        dto.Questions.Should().HaveCount(10);
        dto.IsRead.Should().BeFalse();
        section.IsFilled.Should().BeTrue();
        await _books.Received(1).SaveAsync(book, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSection_first_section_is_accessible_with_no_progress()
    {
        var book = BookWith("One", "Two");
        var section = book.Sections[0];
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns((BookProgress?)null);
        _generator.GenerateAsync(
                book.Title, book.Synopsis, section.Title, section.Order, book.SectionCount, book.Level,
                Arg.Any<CancellationToken>())
            .Returns(GeneratedSection());
        var handler = new GetBookSectionQueryHandler(_books, _progress, _generator);

        var dto = await handler.Handle(
            new GetBookSectionQuery(book.Id, section.Id, Learner), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
    }

    [Fact]
    public async Task GetSection_throws_when_the_previous_section_has_not_been_passed()
    {
        var book = BookWith("One", "Two");
        var secondSection = book.Sections[1];
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns((BookProgress?)null);
        var handler = new GetBookSectionQueryHandler(_books, _progress, _generator);

        var act = () => handler.Handle(
            new GetBookSectionQuery(book.Id, secondSection.Id, Learner), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _generator.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSection_is_accessible_after_the_previous_section_is_passed()
    {
        var book = BookWith("One", "Two");
        var firstSection = book.Sections[0];
        var secondSection = book.Sections[1];
        var progress = BookProgress.Start(Learner, book.Id, Now);
        progress.RecordSection(firstSection.Id, 8, true, book.SectionCount, Now);
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns(progress);
        _generator.GenerateAsync(
                book.Title, book.Synopsis, secondSection.Title, secondSection.Order, book.SectionCount,
                book.Level, Arg.Any<CancellationToken>())
            .Returns(GeneratedSection());
        var handler = new GetBookSectionQueryHandler(_books, _progress, _generator);

        var dto = await handler.Handle(
            new GetBookSectionQuery(book.Id, secondSection.Id, Learner), CancellationToken.None);

        dto.IsReady.Should().BeTrue();
    }

    [Fact]
    public async Task GetSection_stays_pending_when_generation_returns_too_few_questions()
    {
        var book = BookWith("One");
        var section = book.Sections[0];
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns((BookProgress?)null);
        _generator.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CefrLevel>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedBookSection(
                "Body.", new[] { new GeneratedBookQuestion("Q?", new[] { "A", "B" }, 0, null) }));
        var handler = new GetBookSectionQueryHandler(_books, _progress, _generator);

        var dto = await handler.Handle(
            new GetBookSectionQuery(book.Id, section.Id, Learner), CancellationToken.None);

        dto.IsReady.Should().BeFalse();
        section.IsFilled.Should().BeFalse();
    }

    [Fact]
    public void Check_answer_validator_rejects_invalid_identifiers_and_option()
    {
        var result = new CheckBookAnswerCommandValidator().TestValidate(
            new CheckBookAnswerCommand(Guid.Empty, Guid.Empty, Guid.Empty, -1));

        result.ShouldHaveValidationErrorFor(x => x.BookId);
        result.ShouldHaveValidationErrorFor(x => x.SectionId);
        result.ShouldHaveValidationErrorFor(x => x.QuestionId);
        result.ShouldHaveValidationErrorFor(x => x.SelectedOptionIndex);
    }

    [Fact]
    public async Task Check_answer_returns_immediate_authoritative_feedback()
    {
        var book = BookWith("Only");
        var section = book.Sections[0];
        var questions = Enumerable.Range(0, BookSection.QuestionsPerSection)
            .Select(index => BookQuestion.Create($"Q{index}?", new[] { "A", "B", "C" }, 1, "Because B."))
            .ToList();
        section.FillContent("Body text.", questions);
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        var handler = new CheckBookAnswerCommandHandler(_books);

        var correct = await handler.Handle(
            new CheckBookAnswerCommand(book.Id, section.Id, questions[0].Id, 1),
            CancellationToken.None);
        var incorrect = await handler.Handle(
            new CheckBookAnswerCommand(book.Id, section.Id, questions[0].Id, 0),
            CancellationToken.None);

        correct.IsCorrect.Should().BeTrue();
        correct.CorrectOptionIndex.Should().Be(1);
        correct.Explanation.Should().BeNull();
        incorrect.IsCorrect.Should().BeFalse();
        incorrect.CorrectOptionIndex.Should().Be(1);
        incorrect.Explanation.Should().Be("Because B.");
    }

    [Fact]
    public async Task Check_answer_rejects_an_option_outside_the_question()
    {
        var book = BookWith("Only");
        var section = book.Sections[0];
        var questions = Enumerable.Range(0, BookSection.QuestionsPerSection)
            .Select(index => BookQuestion.Create($"Q{index}?", new[] { "A", "B" }, 0))
            .ToList();
        section.FillContent("Body text.", questions);
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        var handler = new CheckBookAnswerCommandHandler(_books);

        var act = () => handler.Handle(
            new CheckBookAnswerCommand(book.Id, section.Id, questions[0].Id, 2),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Submit_passing_marks_the_section_read_and_completes_a_one_section_book()
    {
        var book = BookWith("Only");
        var section = book.Sections[0];
        var questions = new List<BookQuestion>();
        for (var i = 0; i < BookSection.QuestionsPerSection; i++)
            questions.Add(BookQuestion.Create($"Q{i}?", new[] { "A", "B", "C" }, 1, "Because B."));
        section.FillContent("Body text.", questions);

        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns((BookProgress?)null);
        var profile = ProfileAt(CefrLevel.B1);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        var handler = new SubmitBookQuizCommandHandler(
            _books, _progress, _profiles, _dailyProgress, _clock);

        // Answer 8 of 10 correctly (index 1).
        var answers = questions.Take(8)
            .Select(q => new BookQuizAnswer(q.Id, 1))
            .Concat(questions.Skip(8).Select(q => new BookQuizAnswer(q.Id, 0)))
            .ToList();

        var result = await handler.Handle(
            new SubmitBookQuizCommand(book.Id, section.Id, Learner, answers), CancellationToken.None);

        result.CorrectCount.Should().Be(8);
        result.RequiredCorrect.Should().Be(7);
        result.Passed.Should().BeTrue();
        result.BookCompleted.Should().BeTrue();
        result.SectionsRead.Should().Be(1);
        result.SectionCount.Should().Be(1);
        profile.Errors.Should().HaveCount(2);
        profile.Errors.Should().OnlyContain(error =>
            error.Skill == SkillType.Reading && error.Source == "book_quiz");
        await _progress.Received(1).SaveAsync(Arg.Any<BookProgress>(), Arg.Any<CancellationToken>());
        await _profiles.Received(1).TrackAsync(profile, Arg.Any<CancellationToken>());
        await _progress.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _dailyProgress.Received(1).RecordSkillAsync(
            Learner, SkillType.Reading, Arg.Any<int>(), Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_below_threshold_does_not_complete_the_book()
    {
        var book = BookWith("Only");
        var section = book.Sections[0];
        var questions = new List<BookQuestion>();
        for (var i = 0; i < BookSection.QuestionsPerSection; i++)
            questions.Add(BookQuestion.Create($"Q{i}?", new[] { "A", "B", "C" }, 1, "Because B."));
        section.FillContent("Body text.", questions);

        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns((BookProgress?)null);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        var handler = new SubmitBookQuizCommandHandler(
            _books, _progress, _profiles, _dailyProgress, _clock);

        // Answer only 6 of 10 correctly.
        var answers = questions.Take(6)
            .Select(q => new BookQuizAnswer(q.Id, 1))
            .Concat(questions.Skip(6).Select(q => new BookQuizAnswer(q.Id, 0)))
            .ToList();

        var result = await handler.Handle(
            new SubmitBookQuizCommand(book.Id, section.Id, Learner, answers), CancellationToken.None);

        result.CorrectCount.Should().Be(6);
        result.Passed.Should().BeFalse();
        result.BookCompleted.Should().BeFalse();
        result.SectionsRead.Should().Be(0);
        await _progress.Received(1).CommitAsync(Arg.Any<CancellationToken>());
        await _dailyProgress.DidNotReceive().RecordSkillAsync(
            Learner, SkillType.Reading, Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submit_returns_saved_result_when_daily_reward_is_temporarily_unavailable()
    {
        var book = BookWith("Only");
        var section = book.Sections[0];
        var questions = Enumerable.Range(0, BookSection.QuestionsPerSection)
            .Select(index => BookQuestion.Create($"Q{index}?", new[] { "A", "B", "C" }, 1, "Because B."))
            .ToList();
        section.FillContent("Body text.", questions);
        _books.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);
        _progress.GetAsync(Learner, book.Id, Arg.Any<CancellationToken>()).Returns((BookProgress?)null);
        _dailyProgress.RecordSkillAsync(
                Learner, SkillType.Reading, Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<Task<Application.Gamification.Dtos.SkillRewardDto>>(_ => throw new InvalidOperationException("Redis unavailable"));
        var handler = new SubmitBookQuizCommandHandler(
            _books, _progress, _profiles, _dailyProgress, _clock);
        var answers = questions.Select(question => new BookQuizAnswer(question.Id, 1)).ToList();

        var result = await handler.Handle(
            new SubmitBookQuizCommand(book.Id, section.Id, Learner, answers), CancellationToken.None);

        result.Passed.Should().BeTrue();
        result.BookCompleted.Should().BeTrue();
        await _progress.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
