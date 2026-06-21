using Application.Video.Dtos;
using Application.Video.Ports;
using Domain.Video;
using MediatR;

namespace Application.Video.IngestVideo;

public sealed class IngestVideoCommandHandler : IRequestHandler<IngestVideoCommand, VideoLessonDto>
{
    private readonly IYouTubeMetadataProvider _metadata;
    private readonly ICefrVideoLeveler _leveler;
    private readonly IVideoRepository _videos;
    private readonly TimeProvider _clock;

    public IngestVideoCommandHandler(
        IYouTubeMetadataProvider metadata,
        ICefrVideoLeveler leveler,
        IVideoRepository videos,
        TimeProvider clock)
    {
        _metadata = metadata;
        _leveler = leveler;
        _videos = videos;
        _clock = clock;
    }

    public async Task<VideoLessonDto> Handle(IngestVideoCommand request, CancellationToken cancellationToken)
    {
        var metadata = await _metadata.FetchAsync(request.YouTubeVideoId, cancellationToken);

        var transcript = metadata.Transcript.Select(line =>
            TranscriptSegment.Create(line.StartSeconds, line.EndSeconds, line.EnglishText, line.UzbekTranslation));

        var lesson = VideoLesson.Ingest(
            metadata.YouTubeVideoId,
            metadata.Title,
            metadata.Channel,
            metadata.DurationSeconds,
            request.Topic,
            transcript,
            _clock.GetUtcNow());

        var transcriptText = string.Join(" ", metadata.Transcript.Select(l => l.EnglishText));
        var level = await _leveler.EstimateLevelAsync(transcriptText, cancellationToken);
        lesson.AssignLevel(level);

        await _videos.SaveAsync(lesson, cancellationToken);

        return VideoLessonDto.FromDomain(lesson);
    }
}
