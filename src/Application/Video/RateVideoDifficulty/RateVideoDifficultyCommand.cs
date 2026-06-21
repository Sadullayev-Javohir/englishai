using Domain.Video;
using MediatR;

namespace Application.Video.RateVideoDifficulty;

/// <summary>
/// Records a learner's "was it easy/hard?" rating of a video - the recommendation
/// model's training signal (PROJECT-SPEC B.3, Bosqich 2).
/// </summary>
public sealed record RateVideoDifficultyCommand(
    Guid LearnerId,
    Guid VideoLessonId,
    VideoDifficultyRating Rating) : IRequest<Unit>;
