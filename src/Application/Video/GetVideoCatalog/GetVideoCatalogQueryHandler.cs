using Application.Learning.Ports;
using Application.Video.Dtos;
using Application.Video.Ports;
using Domain.Assessment;
using MediatR;

namespace Application.Video.GetVideoCatalog;

public sealed class GetVideoCatalogQueryHandler
    : IRequestHandler<GetVideoCatalogQuery, IReadOnlyList<VideoSummaryDto>>
{
    /// <summary>
    /// How many CEFR bands either side of the learner's level to include. The full CEFR range spans
    /// 6 bands (A1–C2), so 5 covers the entire library from any level: every leveled video is
    /// returned, ordered by closeness to the learner's level (closest first). This keeps the catalog
    /// adaptive - the most relevant videos lead - while letting the learner scroll on to the rest
    /// instead of hitting a ±1-band wall.
    /// </summary>
    private const int LevelTolerance = 5;

    /// <summary>Starting level for a learner who has not taken the placement test yet.</summary>
    private const CefrLevel DefaultLevel = CefrLevel.A2;

    private readonly IVideoRepository _videos;
    private readonly ILearnerProfileRepository _profiles;

    public GetVideoCatalogQueryHandler(IVideoRepository videos, ILearnerProfileRepository profiles)
    {
        _videos = videos;
        _profiles = profiles;
    }

    public async Task<IReadOnlyList<VideoSummaryDto>> Handle(
        GetVideoCatalogQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        var level = profile?.OverallLevel ?? DefaultLevel;

        var lessons = await _videos.GetCatalogForLevelAsync(level, LevelTolerance, cancellationToken);

        return lessons.Select(VideoSummaryDto.FromDomain).ToList();
    }
}
