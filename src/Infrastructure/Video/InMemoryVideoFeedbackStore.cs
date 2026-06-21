using System.Collections.Concurrent;
using Application.Video.Ports;
using Domain.Video;

namespace Infrastructure.Video;

/// <summary>
/// In-memory <see cref="IVideoFeedbackStore"/> for the MVP. Accumulates "easy/hard"
/// ratings (PROJECT-SPEC B.3, Bosqich 2) so the recommendation model can train on them
/// later; swap for durable analytics storage behind the same port when the catalog grows.
/// </summary>
public sealed class InMemoryVideoFeedbackStore : IVideoFeedbackStore
{
    public sealed record Rating(Guid LearnerId, Guid VideoLessonId, VideoDifficultyRating Value, DateTimeOffset RatedAt);

    private readonly ConcurrentBag<Rating> _ratings = new();

    /// <summary>All ratings recorded so far (read-only snapshot).</summary>
    public IReadOnlyCollection<Rating> Ratings => _ratings.ToArray();

    public Task RecordRatingAsync(
        Guid learnerId, Guid videoLessonId, VideoDifficultyRating rating, DateTimeOffset ratedAt,
        CancellationToken cancellationToken = default)
    {
        _ratings.Add(new Rating(learnerId, videoLessonId, rating, ratedAt));
        return Task.CompletedTask;
    }
}
