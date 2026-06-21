using Application.Writing.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Writing.GetWritingCatalog;

/// <summary>
/// Returns the writing task list (PROJECT-SPEC G.3). The view depends on the optional filters:
/// <see cref="AllLevels"/> returns the whole catalog ordered easiest-first so the learner can write
/// at every level; an explicit <see cref="Level"/> returns just that level (so the learner can browse
/// any single level); otherwise the list is adapted to the learner's own CEFR level (a default level
/// when the learner has no profile yet).
/// </summary>
public sealed record GetWritingCatalogQuery(
    Guid LearnerId,
    CefrLevel? Level = null,
    bool AllLevels = false) : IRequest<IReadOnlyList<WritingTaskSummaryDto>>;
