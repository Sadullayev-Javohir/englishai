using Application.Video.Dtos;
using MediatR;

namespace Application.Video.GetVideoFeed;

/// <summary>
/// Returns one page of the adaptive, infinitely-scrollable video feed for a learner
/// (PROJECT-SPEC B.3, Bosqich 2). The first call passes a <c>null</c> <see cref="Cursor"/>
/// and gets a level-appropriate search; each response carries a <c>NextCursor</c> the client
/// sends back to load the next page as the user scrolls down.
/// </summary>
public sealed record GetVideoFeedQuery(Guid LearnerId, string? Cursor, int PageSize, string? VisitSeed = null)
    : IRequest<VideoFeedDto>;
