using Application.Books.Dtos;
using Application.Books.Ports;
using Application.Common;
using Application.Learning.Ports;
using Domain.Assessment;
using Domain.Books;
using MediatR;

namespace Application.Books.GetBooksCatalog;

/// <summary>
/// Returns the Books library for the requested view, folding in each book's per-learner progress
/// and lazily resolving cover images once (docs/development-guide.md rule 12 - a licensed image is fetched and
/// persisted on the book the first time it is shown, then reused).
/// </summary>
public sealed class GetBooksCatalogQueryHandler
    : IRequestHandler<GetBooksCatalogQuery, IReadOnlyList<BookSummaryDto>>
{
    /// <summary>Starting level for a learner who has not taken the placement test yet.</summary>
    private const CefrLevel DefaultLevel = CefrLevel.A2;

    private readonly IBookRepository _books;
    private readonly IBookProgressStore _progress;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IImageService _images;

    public GetBooksCatalogQueryHandler(
        IBookRepository books,
        IBookProgressStore progress,
        ILearnerProfileRepository profiles,
        IImageService images)
    {
        _books = books;
        _progress = progress;
        _profiles = profiles;
        _images = images;
    }

    public async Task<IReadOnlyList<BookSummaryDto>> Handle(
        GetBooksCatalogQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Book> books;
        if (request.AllLevels)
        {
            books = await _books.GetAllAsync(cancellationToken);
        }
        else
        {
            CefrLevel level;
            if (request.Level.HasValue)
            {
                level = request.Level.Value;
            }
            else
            {
                var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
                level = profile?.OverallLevel ?? DefaultLevel;
            }

            books = await _books.GetByLevelAsync(level, cancellationToken);
        }

        await EnsureCoversAsync(books, cancellationToken);

        var progress = (await _progress.GetByLearnerAsync(request.LearnerId, cancellationToken))
            .ToDictionary(p => p.BookId);

        return books
            .Select(b => BookSummaryDto.FromDomain(b, progress.GetValueOrDefault(b.Id)))
            .ToList();
    }

    // Lazily resolves and persists a cover image for any book that does not have one yet. The image
    // service returns null when unavailable (no key/no match), in which case the book stays
    // cover-less and the UI renders a generated placeholder - never a blocking failure (rule 12).
    private async Task EnsureCoversAsync(IReadOnlyList<Book> books, CancellationToken cancellationToken)
    {
        foreach (var book in books.Where(b => !b.HasCover))
        {
            var image = await _images.FindImageAsync(book.CoverImageQuery, cancellationToken);
            if (image is null)
                continue;

            book.SetCover(image.Url, image.Attribution);
            await _books.SaveAsync(book, cancellationToken);
        }
    }
}
