using Domain.Video;

namespace Application.Video.Ports;

/// <summary>
/// Records a learner's "easy/hard" rating of a video - the training signal for the
/// recommendation model (PROJECT-SPEC B.3, Bosqich 2). Kept behind a port so the MVP
/// in-memory store can later be swapped for durable analytics storage.
/// </summary>
public interface IVideoFeedbackStore
{
    Task RecordRatingAsync(
        Guid learnerId, Guid videoLessonId, VideoDifficultyRating rating, DateTimeOffset ratedAt,
        CancellationToken cancellationToken = default);
}
