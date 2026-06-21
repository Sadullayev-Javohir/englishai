using Application.Common;
using Application.Video.Dtos;
using Application.Video.Ports;
using MediatR;

namespace Application.Video.GetVideoLesson;

public sealed class GetVideoLessonQueryHandler : IRequestHandler<GetVideoLessonQuery, VideoLessonDto>
{
    private readonly IVideoRepository _videos;
    private readonly IVideoTranscriptFiller _transcriptFiller;

    public GetVideoLessonQueryHandler(IVideoRepository videos, IVideoTranscriptFiller transcriptFiller)
    {
        _videos = videos;
        _transcriptFiller = transcriptFiller;
    }

    public async Task<VideoLessonDto> Handle(GetVideoLessonQuery request, CancellationToken cancellationToken)
    {
        var lesson = await _videos.GetByIdAsync(request.VideoLessonId, cancellationToken)
                     ?? throw new NotFoundException("Video lesson", request.VideoLessonId);

        // The interactive transcript is filled lazily from the video's real captions the first
        // time a lesson is opened with none (PROJECT-SPEC B.3, Bosqich 3). The fetch + translate
        // is kicked off in the background so this read returns immediately and the player loads
        // fast; the client polls for the transcript to appear. A blocked/uncaptioned video simply
        // yields nothing, so the player keeps its honest "pending" state (rules 8, 11).
        // A terminally unavailable transcript has already been checked by every configured source.
        // Do not turn normal client polling into a new fetch on every GET. Pending is the only
        // state that should enqueue a lazy fill; Partial/Available already have visible lines.
        if (lesson.TranscriptStatus == Domain.Video.TranscriptStatus.Pending && lesson.Transcript.Count == 0)
            _transcriptFiller.RequestFill(lesson.Id);

        return VideoLessonDto.FromDomain(lesson);
    }
}
