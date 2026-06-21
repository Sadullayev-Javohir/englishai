using Application.Video.Dtos;
using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;
using MediatR;

namespace Application.Video.OpenVideoByUrl;

public sealed class OpenVideoByUrlCommandHandler : IRequestHandler<OpenVideoByUrlCommand, VideoLessonDto>
{
    /// <summary>Topic stored for a learner-pasted video - it has no curated feed topic.</summary>
    private const string UserAddedTopic = "User added video";

    private readonly IVideoRepository _videos;
    private readonly IYouTubeMetadataProvider _metadata;
    private readonly ICefrVideoLeveler _leveler;
    private readonly TimeProvider _clock;

    public OpenVideoByUrlCommandHandler(
        IVideoRepository videos,
        IYouTubeMetadataProvider metadata,
        ICefrVideoLeveler leveler,
        TimeProvider clock)
    {
        _videos = videos;
        _metadata = metadata;
        _leveler = leveler;
        _clock = clock;
    }

    public async Task<VideoLessonDto> Handle(OpenVideoByUrlCommand request, CancellationToken cancellationToken)
    {
        var existing = await _videos.GetByYouTubeIdAsync(request.YouTubeVideoId, cancellationToken);
        if (existing is not null)
            return VideoLessonDto.FromDomain(existing);

        // Fetch the video's real title/channel/duration. Resilient: a missing API key or an
        // unreachable video falls back to honest, minimal metadata (rule 8 - no fabricated
        // facts) so the video still opens and its transcript fills lazily from the real
        // captions (yt-dlp), exactly like the feed open flow.
        var title = "YouTube video";
        var channel = "YouTube";
        var durationSeconds = 1;
        var level = CefrLevel.A2;

        try
        {
            var metadata = await _metadata.FetchAsync(request.YouTubeVideoId, cancellationToken);
            title = metadata.Title;
            channel = metadata.Channel;
            durationSeconds = Math.Max(1, metadata.DurationSeconds);

            var transcriptText = string.Join(" ", metadata.Transcript.Select(l => l.EnglishText));
            if (!string.IsNullOrWhiteSpace(transcriptText))
                level = await _leveler.EstimateLevelAsync(transcriptText, cancellationToken);
        }
        catch
        {
            // Keep the honest fallback metadata; the video is still playable and its transcript
            // is filled from real captions on open.
        }

        var lesson = VideoLesson.Curate(
            request.YouTubeVideoId,
            title,
            channel,
            durationSeconds,
            UserAddedTopic,
            level,
            Array.Empty<TranscriptSegment>(),
            Array.Empty<ComprehensionQuestion>(),
            _clock.GetUtcNow());

        await _videos.SaveAsync(lesson, cancellationToken);

        return VideoLessonDto.FromDomain(lesson);
    }
}
