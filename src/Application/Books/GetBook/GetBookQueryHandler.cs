using Application.Books.Dtos;
using Application.Books.Ports;
using Application.Common;
using Domain.Books;
using MediatR;

namespace Application.Books.GetBook;

/// <summary>Loads a book and the learner's progress, projecting the detail + table of contents.</summary>
public sealed class GetBookQueryHandler : IRequestHandler<GetBookQuery, BookDetailDto>
{
    private readonly IBookRepository _books;
    private readonly IBookProgressStore _progress;

    public GetBookQueryHandler(IBookRepository books, IBookProgressStore progress)
    {
        _books = books;
        _progress = progress;
    }

    public async Task<BookDetailDto> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(request.BookId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Book), request.BookId);

        var progress = await _progress.GetAsync(request.LearnerId, book.Id, cancellationToken);

        return BookDetailDto.FromDomain(book, progress);
    }
}
