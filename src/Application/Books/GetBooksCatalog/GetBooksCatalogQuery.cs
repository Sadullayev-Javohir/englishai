using Application.Books.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Books.GetBooksCatalog;

/// <summary>
/// Returns the Books library. With no filter it adapts to the learner's own level; an explicit
/// <see cref="Level"/> browses that single CEFR band; <see cref="AllLevels"/> returns the whole
/// A1→C2 library easiest-first. Each row carries the learner's progress (sections read, completed).
/// </summary>
public sealed record GetBooksCatalogQuery(
    Guid LearnerId,
    CefrLevel? Level,
    bool AllLevels) : IRequest<IReadOnlyList<BookSummaryDto>>;
