using Application.Common;
using Application.Video.Ports;
using MediatR;

namespace Application.Video.RateVideoDifficulty;

public sealed class RateVideoDifficultyCommandHandler : IRequestHandler<RateVideoDifficultyCommand, Unit>
{
    private readonly IVideoRepository _videos;
    private readonly IVideoFeedbackStore _feedback;
    private readonly TimeProvider _clock;

    public RateVideoDifficultyCommandHandler(
        IVideoRepository videos, IVideoFeedbackStore feedback, TimeProvider clock)
    {
        _videos = videos;
        _feedback = feedback;
        _clock = clock;
    }

    public async Task<Unit> Handle(RateVideoDifficultyCommand request, CancellationToken cancellationToken)
    {
        var lesson = await _videos.GetByIdAsync(request.VideoLessonId, cancellationToken)
                     ?? throw new NotFoundException("Video lesson", request.VideoLessonId);

        await _feedback.RecordRatingAsync(
            request.LearnerId, lesson.Id, request.Rating, _clock.GetUtcNow(), cancellationToken);

        return Unit.Value;
    }
}
