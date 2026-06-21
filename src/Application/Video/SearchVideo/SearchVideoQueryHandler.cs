using Application.Video.Dtos;
using Application.Video.Models;
using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;
using MediatR;

namespace Application.Video.SearchVideo;

/// <summary>
/// Handles <see cref="SearchVideoQuery"/>: runs the learner's query against the YouTube feed
/// source (English-only, safe-search strict) and maps results into feed DTOs. Pagination is
/// driven by an opaque cursor that round-trips the search term + source continuation token.
/// Adult/pornographic results are filtered out via a hard blacklist as a final safety net.
/// </summary>
public sealed class SearchVideoQueryHandler : IRequestHandler<SearchVideoQuery, VideoFeedDto>
{
    /// <summary>Topic tag shown on search-result cards.</summary>
    private const string SearchTopic = "search";

    private const CefrLevel DefaultLevel = CefrLevel.A2;

    // Hard blacklist of adult/pornographic signals. A result whose title OR channel contains any
    // of these (case-insensitive, whole-word-ish) is dropped so it can never reach the learner.
    private static readonly string[] AdultBlacklist =
    {
        "porn", "porno", "xxx", "xnxx", "xvideos", "xhamster", "sex tape", "onlyfans",
        "nude", "nudes", "erotic", "erotica", "adult film", "adult movie", "hot girl",
        "hot girls", "escort", "fetish", "hentai", "boobs", "tits", "pussy", "dick",
        "naked", "naked video", "nsfw", "strip", "stripper", "camgirl", "webcam sex",
    };

    private readonly IVideoFeedSource _feed;
    private readonly IVideoRepository _videos;

    public SearchVideoQueryHandler(IVideoFeedSource feed, IVideoRepository videos)
    {
        _feed = feed;
        _videos = videos;
    }

    public async Task<VideoFeedDto> Handle(SearchVideoQuery request, CancellationToken cancellationToken)
    {
        var cursor = SearchCursor.TryDecode(request.Cursor);

        string searchTerm;
        string? continuation;

        if (cursor is null)
        {
            // Force English-only learning content: append English cues so the source biases
            // toward English-language lessons, and rely on the source's relevanceLanguage=en +
            // safeSearch=strict filters.
            searchTerm = ForceEnglishQuery(request.Query);
            continuation = null;
        }
        else
        {
            searchTerm = cursor.SearchTerm;
            continuation = cursor.Continuation;
        }

        var page = await _feed.SearchAsync(searchTerm, continuation, request.PageSize, cancellationToken);

        var safeItems = page.Items.Where(IsSafe).ToList();

        var ids = safeItems.Select(i => i.YouTubeVideoId).ToArray();
        var existing = ids.Length == 0
            ? new Dictionary<string, VideoLesson>()
            : await _videos.GetByYouTubeIdsAsync(ids, cancellationToken);

        var items = safeItems.Select(item =>
        {
            existing.TryGetValue(item.YouTubeVideoId, out var lesson);
            return new VideoFeedItemDto(
                lesson?.Id,
                item.YouTubeVideoId,
                item.Title,
                item.Channel,
                item.DurationSeconds,
                SearchTopic,
                lesson?.Level ?? DefaultLevel,
                item.HasClosedCaptions || lesson?.Transcript.Count > 0,
                item.ChannelAvatarUrl);
        }).ToList();

        var nextCursor = page.NextContinuation is null
            ? null
            : new SearchCursor(searchTerm, page.NextContinuation).Encode();

        return new VideoFeedDto(items, nextCursor);
    }

    /// <summary>Appends English-learning cues so results stay English-only and on-topic.</summary>
    private static string ForceEnglishQuery(string query)
    {
        var trimmed = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return "english learning video";
        return $"{trimmed} english";
    }

    /// <summary>True when neither the title nor channel matches an adult/pornographic blacklist term.</summary>
    private static bool IsSafe(VideoFeedResult item)
    {
        var haystack = $"{item.Title} {item.Channel}".ToLowerInvariant();
        return !AdultBlacklist.Any(bad => haystack.Contains(bad));
    }

}
