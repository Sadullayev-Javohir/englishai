using Application.Video.Dtos;
using MediatR;

namespace Application.Video.SearchVideo;

/// <summary>
/// Searches YouTube for English-learning videos by a learner-typed query (the catalog's
/// search box). The query is forced to English-only results and safe-search is strict on the
/// source side (docs/development-guide.md rule 11 / PROJECT-SPEC Faza 4); this query additionally drops any
/// adult/pornographic titles via a hard blacklist so the feed can never surface such content.
/// Returns a page of feed items plus an opaque cursor to load more results for the same query.
/// </summary>
public sealed record SearchVideoQuery(string Query, string? Cursor, int PageSize)
    : IRequest<VideoFeedDto>;
