using Application.Books.Dtos;
using MediatR;

namespace Application.Books.GetBook;

/// <summary>
/// Returns a book's detail page: metadata, synopsis, cover and its table of contents with this
/// learner's per-section read state. The section bodies are not generated here - each is filled
/// lazily when the learner opens it.
/// </summary>
public sealed record GetBookQuery(Guid BookId, Guid LearnerId) : IRequest<BookDetailDto>;
