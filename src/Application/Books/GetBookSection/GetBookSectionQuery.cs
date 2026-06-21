using Application.Books.Dtos;
using MediatR;

namespace Application.Books.GetBookSection;

/// <summary>
/// Returns one section of a book for the reader: its English body and the (answerless) ten-question
/// comprehension quiz. Generated and cached on first open; while pending, the detail comes back
/// with <c>IsReady=false</c>. The learner id is used to report whether they have already passed it.
/// </summary>
public sealed record GetBookSectionQuery(Guid BookId, Guid SectionId, Guid LearnerId)
    : IRequest<BookSectionDto>;
