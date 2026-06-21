using Domain.Assessment;
using Domain.Video;

namespace Application.Video.Ports;

/// <summary>
/// Persistence port for the <see cref="VideoLesson"/> aggregate. Implemented by an EF Core
/// adapter (PostgreSQL) in production and an in-memory adapter (seeded with the curated
/// catalog) for dev/tests.
/// </summary>
public interface IVideoRepository
{
    /// <summary>Returns a single lesson by id, or <c>null</c> if it does not exist.</summary>
    Task<VideoLesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Returns a stored lesson by its YouTube video id, or <c>null</c> if none.</summary>
    Task<VideoLesson?> GetByYouTubeIdAsync(string youTubeVideoId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the stored lessons whose YouTube video id is in <paramref name="youTubeVideoIds"/>,
    /// keyed by that id - used to mark which feed results are already real lessons.
    /// </summary>
    Task<IReadOnlyDictionary<string, VideoLesson>> GetByYouTubeIdsAsync(
        IReadOnlyCollection<string> youTubeVideoIds, CancellationToken cancellationToken);

    /// <summary>
    /// Leveled lessons whose CEFR band is within <paramref name="levelTolerance"/> of
    /// <paramref name="level"/>, ordered by closeness to the learner's level - the
    /// adaptive curation in PROJECT-SPEC B.3 (Bosqich 2).
    /// </summary>
    Task<IReadOnlyList<VideoLesson>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken, int limit = 100);

    /// <summary>Inserts a new lesson or updates an existing one.</summary>
    Task SaveAsync(VideoLesson lesson, CancellationToken cancellationToken);
}
