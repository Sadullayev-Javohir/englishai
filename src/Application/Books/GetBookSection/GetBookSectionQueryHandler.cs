using Application.Books.Dtos;
using Application.Books.Ports;
using Application.Common;
using Domain.Books;
using Domain.Common;
using MediatR;

namespace Application.Books.GetBookSection;

/// <summary>
/// Resolves one book section, generating and caching its body + ten comprehension questions on
/// first open (the same lazy-fill pattern as a reading lesson). A failed or partial generation
/// leaves the section honestly pending (<c>IsReady=false</c>) rather than fabricating content.
/// </summary>
public sealed class GetBookSectionQueryHandler : IRequestHandler<GetBookSectionQuery, BookSectionDto>
{
    private readonly IBookRepository _books;
    private readonly IBookProgressStore _progress;
    private readonly IBookContentGenerator _generator;

    public GetBookSectionQueryHandler(
        IBookRepository books, IBookProgressStore progress, IBookContentGenerator generator)
    {
        _books = books;
        _progress = progress;
        _generator = generator;
    }

    public async Task<BookSectionDto> Handle(GetBookSectionQuery request, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(request.BookId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Book), request.BookId);

        var section = book.FindSection(request.SectionId)
                      ?? throw new NotFoundException(nameof(BookSection), request.SectionId);

        var progress = await _progress.GetAsync(request.LearnerId, book.Id, cancellationToken);
        var isRead = progress?.HasPassed(section.Id) ?? false;

        // Sequential unlocking: a section (other than the first) stays locked until the section
        // immediately before it has been passed, mirroring the Level Map's topic-order gate.
        var previousSection = book.Sections
            .Where(s => s.Order < section.Order)
            .OrderByDescending(s => s.Order)
            .FirstOrDefault();
        if (previousSection is not null && progress?.HasPassed(previousSection.Id) != true)
            throw new ForbiddenException("Complete the previous section before opening this one.");

        if (!section.IsFilled)
            await TryGenerateAsync(book, section, cancellationToken);

        return section.IsFilled
            ? BookSectionDto.FromDomain(book, section, isRead)
            : BookSectionDto.Pending(book, section, isRead);
    }

    // Best-effort lazy fill: generate the section body and exactly ten questions and cache them.
    // Any failure leaves the section honestly "pending" rather than fabricating content (rules 8, 11).
    private async Task TryGenerateAsync(Book book, BookSection section, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _generator.GenerateAsync(
                book.Title, book.Synopsis, section.Title, section.Order, book.SectionCount, book.Level,
                cancellationToken);
            if (!content.HasContent)
                return;

            var questions = content.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Prompt) && q.Options.Count >= BookQuestion.MinOptions)
                .Select(q => BookQuestion.Create(q.Prompt, q.Options, q.CorrectOptionIndex, q.Explanation))
                .ToList();

            // A filled section needs exactly ten valid questions for a consistent comprehension quiz. Anything less
            // stays pending so the learner retries generation rather than seeing a short quiz.
            if (questions.Count != BookSection.QuestionsPerSection)
                return;

            section.FillContent(content.Body, questions);
            await _books.SaveAsync(book, cancellationToken);
        }
        catch (DomainException)
        {
            // Generated content failed a domain invariant - keep the section pending.
        }
    }
}
