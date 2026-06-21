using Application.Reading.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Reading.GetReadingCatalog;

/// <summary>
/// Returns the reading list (PROJECT-SPEC Faza 6 - leveled text). The view depends on the optional
/// filters: <see cref="AllLevels"/> returns the whole catalog ordered easiest-first so the learner
/// can read every level; an explicit <see cref="Level"/> returns just that level (so the learner can
/// browse any single level); otherwise the list is adapted to the learner's own CEFR level (a default
/// level when the learner has no profile yet).
/// </summary>
public sealed record GetReadingCatalogQuery(
    Guid LearnerId,
    CefrLevel? Level = null,
    bool AllLevels = false) : IRequest<IReadOnlyList<ReadingSummaryDto>>;
