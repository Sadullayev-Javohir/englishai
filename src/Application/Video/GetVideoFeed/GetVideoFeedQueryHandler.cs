using Application.Learning.Ports;
using Application.Video.Dtos;
using Application.Video.Ports;
using Domain.Assessment;
using MediatR;

namespace Application.Video.GetVideoFeed;

/// <summary>
/// Builds a page of the infinite video feed. The learner's CEFR level chooses a search term
/// on the first page; thereafter the source's continuation token (carried in the cursor)
/// drives pagination. Results already stored as lessons are tagged with their lesson id so
/// the client can open the full transcript/quiz flow instead of an on-demand open.
/// </summary>
public sealed class GetVideoFeedQueryHandler : IRequestHandler<GetVideoFeedQuery, VideoFeedDto>
{
    /// <summary>Starting level for a learner who has not taken the placement test yet.</summary>
    private const CefrLevel DefaultLevel = CefrLevel.A2;

    /// <summary>Topic tag shown on feed cards (these are listening-practice searches).</summary>
    private const string FeedTopic = "listening";

    private readonly IVideoFeedSource _feed;
    private readonly IVideoRepository _videos;
    private readonly ILearnerProfileRepository _profiles;

    public GetVideoFeedQueryHandler(
        IVideoFeedSource feed, IVideoRepository videos, ILearnerProfileRepository profiles)
    {
        _feed = feed;
        _videos = videos;
        _profiles = profiles;
    }

    public async Task<VideoFeedDto> Handle(GetVideoFeedQuery request, CancellationToken cancellationToken)
    {
        var cursor = FeedCursor.TryDecode(request.Cursor);

        CefrLevel level;
        string searchTerm;
        string? continuation;

        if (cursor is null)
        {
            var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
            level = profile?.OverallLevel ?? DefaultLevel;
            searchTerm = SearchTermFor(level, request.VisitSeed);
            continuation = null;
        }
        else
        {
            searchTerm = cursor.SearchTerm;
            level = LevelForSearchTerm(searchTerm);
            continuation = cursor.Continuation;
        }

        var page = await _feed.SearchAsync(searchTerm, continuation, request.PageSize, cancellationToken);

        var ids = page.Items.Select(i => i.YouTubeVideoId).ToArray();
        var existing = ids.Length == 0
            ? new Dictionary<string, Domain.Video.VideoLesson>()
            : await _videos.GetByYouTubeIdsAsync(ids, cancellationToken);

        var items = page.Items.Select(item =>
        {
            existing.TryGetValue(item.YouTubeVideoId, out var lesson);
            return new VideoFeedItemDto(
                lesson?.Id,
                item.YouTubeVideoId,
                item.Title,
                item.Channel,
                item.DurationSeconds,
                FeedTopic,
                lesson?.Level ?? level,
                item.HasClosedCaptions || lesson?.Transcript.Count > 0,
                item.ChannelAvatarUrl);
        }).ToList();

        var nextCursor = page.NextContinuation is null
            ? null
            : new FeedCursor(searchTerm, page.NextContinuation).Encode();

        return new VideoFeedDto(items, nextCursor);
    }

    /// <summary>A level-appropriate English search term biased toward subtitled lessons.</summary>
    private static string SearchTermFor(CefrLevel level, string? visitSeed)
    {
        var searchTerms = level switch
        {
            CefrLevel.A1 => new[]
            {
                "easy english listening practice for beginners subtitles",
                "beginner english conversation a1 subtitles",
                "basic english stories a1 listening subtitles",
            },
            CefrLevel.A2 => new[]
            {
                "elementary english listening practice a2 subtitles",
                "a2 english conversation practice subtitles",
                "easy english stories a2 listening subtitles",
            },
            CefrLevel.B1 => new[]
            {
                "intermediate english listening practice b1 subtitles",
                "b1 english conversation practice subtitles",
                "english stories for intermediate learners b1 subtitles",
            },
            CefrLevel.B2 => new[]
            {
                "upper intermediate english listening practice b2 subtitles",
                "b2 english conversation practice subtitles",
                "english documentaries for b2 learners subtitles",
            },
            CefrLevel.C1 => new[]
            {
                "advanced english listening practice c1 subtitles",
                "c1 english conversation and vocabulary subtitles",
                "advanced english talks c1 subtitles",
            },
            CefrLevel.C2 => new[]
            {
                "advanced english listening practice c2 subtitles",
                "c2 english conversation practice subtitles",
                "proficiency english talks c2 subtitles",
            },
            _ => new[]
            {
                "english listening practice subtitles",
                "english conversation practice subtitles",
                "english learning stories subtitles",
            },
        };

        return searchTerms[StableIndex(visitSeed, searchTerms.Length)];
    }

    private static int StableIndex(string? value, int length)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;

        var hash = 17;
        foreach (var character in value)
        {
            hash = unchecked(hash * 31 + character);
        }

        return (hash & int.MaxValue) % length;
    }

    /// <summary>Recovers the level a continued search was built for (for card labelling).</summary>
    private static CefrLevel LevelForSearchTerm(string searchTerm) => searchTerm switch
    {
        var s when s.Contains("beginners") => CefrLevel.A1,
        var s when s.Contains("a2") => CefrLevel.A2,
        var s when s.Contains("b1") => CefrLevel.B1,
        var s when s.Contains("b2") => CefrLevel.B2,
        var s when s.Contains("c1") => CefrLevel.C1,
        var s when s.Contains("c2") => CefrLevel.C2,
        _ => DefaultLevel,
    };
}
