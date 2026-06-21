using Application.Video.Dtos;
using MediatR;

namespace Application.Video.GetVideoCatalog;

/// <summary>
/// Returns the adaptive video catalog for a learner - lessons near their CEFR level
/// (PROJECT-SPEC B.3, Bosqich 2). Learners with no profile yet get an A2-centred list.
/// </summary>
public sealed record GetVideoCatalogQuery(Guid LearnerId) : IRequest<IReadOnlyList<VideoSummaryDto>>;
