using Application.Listening.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Listening.GetListeningCatalog;

/// <summary>
/// Returns the listening list (PROJECT-SPEC Faza 4 - leveled audio). The view depends on the
/// optional filters: <see cref="AllLevels"/> returns the whole catalog ordered easiest-first so the
/// learner can practise every level; an explicit <see cref="Level"/> returns just that level (so the
/// learner can browse any single level); otherwise the list is adapted to the learner's own CEFR
/// level (a default level when the learner has no profile yet).
/// </summary>
public sealed record GetListeningCatalogQuery(
    Guid LearnerId,
    CefrLevel? Level = null,
    bool AllLevels = false) : IRequest<IReadOnlyList<ListeningSummaryDto>>;
