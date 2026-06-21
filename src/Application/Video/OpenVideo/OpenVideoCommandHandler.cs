using Application.Video.Dtos;
using Application.Video.Ports;
using Domain.Video;
using MediatR;

namespace Application.Video.OpenVideo;

public sealed class OpenVideoCommandHandler : IRequestHandler<OpenVideoCommand, VideoLessonDto>
{
    private readonly IVideoRepository _videos;
    private readonly TimeProvider _clock;

    public OpenVideoCommandHandler(IVideoRepository videos, TimeProvider clock)
    {
        _videos = videos;
        _clock = clock;
    }

    public async Task<VideoLessonDto> Handle(OpenVideoCommand request, CancellationToken cancellationToken)
    {
        var existing = await _videos.GetByYouTubeIdAsync(request.YouTubeVideoId, cancellationToken);
        if (existing is not null)
            return VideoLessonDto.FromDomain(existing);

        // Persist as a playable, honest lesson: real search metadata, no fabricated
        // transcript/quiz (filled from real captions in Bosqich 3). Level is the band the
        // feed search targeted, so the card stays accurate.
        var lesson = VideoLesson.Curate(
            request.YouTubeVideoId,
            request.Title,
            request.Channel,
            request.DurationSeconds,
            request.Topic,
            request.Level,
            Array.Empty<TranscriptSegment>(),
            Array.Empty<ComprehensionQuestion>(),
            _clock.GetUtcNow());

        await _videos.SaveAsync(lesson, cancellationToken);

        return VideoLessonDto.FromDomain(lesson);
    }
}
