using Domain.Assessment;
using Domain.Books;

namespace Application.Books.Dtos;

/// <summary>
/// A library row for one book, with the learner's progress folded in: how many of its sections
/// they have passed and whether the whole book is confirmed read.
/// </summary>
public sealed record BookSummaryDto(
    Guid Id,
    string Title,
    string TitleUz,
    string Author,
    CefrLevel Level,
    string Topic,
    string? CoverImageUrl,
    string? CoverAttribution,
    int SectionCount,
    int SectionsRead,
    bool IsCompleted)
{
    public static BookSummaryDto FromDomain(Book book, BookProgress? progress) =>
        new(book.Id, book.Title, book.TitleUz, book.Author, book.Level, book.Topic,
            book.CoverImageUrl, book.CoverAttribution, book.SectionCount,
            progress?.PassedSectionCount ?? 0, progress?.IsCompleted ?? false);
}

/// <summary>One section in a book's table of contents, with the learner's read state.</summary>
public sealed record BookSectionSummaryDto(
    Guid Id,
    int Order,
    string Title,
    bool IsRead,
    bool IsLocked)
{
    public static BookSectionSummaryDto FromDomain(BookSection section, bool isRead, bool isLocked) =>
        new(section.Id, section.Order, section.Title, isRead, isLocked);
}

/// <summary>
/// A book's detail page: metadata, synopsis, cover and its ordered table of contents with per-
/// section read state for this learner. A book is "read" once every section is passed.
/// </summary>
public sealed record BookDetailDto(
    Guid Id,
    string Title,
    string TitleUz,
    string Author,
    string Synopsis,
    CefrLevel Level,
    string Topic,
    string? CoverImageUrl,
    string? CoverAttribution,
    int SectionsRead,
    bool IsCompleted,
    IReadOnlyList<BookSectionSummaryDto> Sections)
{
    public static BookDetailDto FromDomain(Book book, BookProgress? progress)
    {
        var orderedSections = book.Sections.OrderBy(section => section.Order).ToList();
        var sections = orderedSections
            .Select((section, index) => BookSectionSummaryDto.FromDomain(
                section,
                progress?.HasPassed(section.Id) ?? false,
                index > 0 && progress?.HasPassed(orderedSections[index - 1].Id) != true))
            .ToList();

        return new BookDetailDto(
            book.Id, book.Title, book.TitleUz, book.Author, book.Synopsis, book.Level, book.Topic,
            book.CoverImageUrl, book.CoverAttribution,
            progress?.PassedSectionCount ?? 0, progress?.IsCompleted ?? false, sections);
    }
}

/// <summary>
/// A quiz question as shown to the learner - the correct answer is intentionally omitted so
/// grading happens server-side (revealed only in the <see cref="BookQuizResultDto"/>).
/// </summary>
public sealed record BookQuestionDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<string> Options)
{
    public static BookQuestionDto FromDomain(BookQuestion question) =>
        new(question.Id, question.Prompt, question.Options);
}

/// <summary>
/// One section's reader detail: the English body and (answerless) ten-question quiz. When
/// <see cref="IsReady"/> is false the section is still being generated, so the reader shows an
/// honest "preparing" state rather than fabricated text (rule 8).
/// </summary>
public sealed record BookSectionDto(
    Guid BookId,
    Guid SectionId,
    int Order,
    string BookTitle,
    string Title,
    string Body,
    CefrLevel Level,
    int WordCount,
    bool IsReady,
    bool IsRead,
    IReadOnlyList<BookQuestionDto> Questions)
{
    public static BookSectionDto FromDomain(Book book, BookSection section, bool isRead) =>
        new(book.Id, section.Id, section.Order, book.Title, section.Title, section.Body, book.Level,
            section.WordCount, section.IsFilled, isRead,
            section.Questions.Select(BookQuestionDto.FromDomain).ToList());

    /// <summary>A pending placeholder for a section whose content is not generated yet.</summary>
    public static BookSectionDto Pending(Book book, BookSection section, bool isRead) =>
        new(book.Id, section.Id, section.Order, book.Title, section.Title, string.Empty, book.Level,
            0, false, isRead, Array.Empty<BookQuestionDto>());
}

/// <summary>The graded outcome of one question, with the correct answer revealed immediately.</summary>
public sealed record BookQuestionOutcomeDto(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Explanation);

/// <summary>
/// The result of submitting a book section's comprehension quiz. <see cref="Passed"/> is true at
/// 70% correct - then the section is marked read; <see cref="BookCompleted"/> turns true once every
/// section of the book has been passed. Below the threshold the learner is asked to retry.
/// </summary>
public sealed record BookQuizResultDto(
    Guid BookId,
    Guid SectionId,
    int TotalQuestions,
    int CorrectCount,
    int RequiredCorrect,
    int ScorePercent,
    bool Passed,
    bool BookCompleted,
    int SectionsRead,
    int SectionCount,
    IReadOnlyList<BookQuestionOutcomeDto> Outcomes);
