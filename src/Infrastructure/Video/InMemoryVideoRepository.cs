using System.Collections.Concurrent;
using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;

namespace Infrastructure.Video;

/// <summary>
/// In-memory <see cref="IVideoRepository"/> for dev/tests and for running the app
/// without a database. Seeded with the curated catalog (<see cref="VideoCatalogSeed"/>)
/// so the catalog/detail/quiz endpoints have content to serve. Stores the live aggregate
/// instance keyed by id, so in-place mutations are reflected on the next read.
/// </summary>
public sealed class InMemoryVideoRepository : IVideoRepository
{
    private readonly ConcurrentDictionary<Guid, VideoLesson> _lessons = new();

    public InMemoryVideoRepository()
        : this(VideoCatalogSeed.Lessons())
    {
    }

    // Parameter type is IReadOnlyList (not IEnumerable) so the DI container cannot
    // satisfy it and falls back to the seeded parameterless constructor.
    public InMemoryVideoRepository(IReadOnlyList<VideoLesson> seed)
    {
        foreach (var lesson in seed)
            _lessons[lesson.Id] = lesson;
    }

    public Task<VideoLesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_lessons.TryGetValue(id, out var lesson) ? lesson : null);

    public Task<VideoLesson?> GetByYouTubeIdAsync(string youTubeVideoId, CancellationToken cancellationToken) =>
        Task.FromResult(_lessons.Values.FirstOrDefault(v => v.YouTubeVideoId == youTubeVideoId));

    public Task<IReadOnlyDictionary<string, VideoLesson>> GetByYouTubeIdsAsync(
        IReadOnlyCollection<string> youTubeVideoIds, CancellationToken cancellationToken)
    {
        var wanted = new HashSet<string>(youTubeVideoIds);
        IReadOnlyDictionary<string, VideoLesson> result = _lessons.Values
            .Where(v => wanted.Contains(v.YouTubeVideoId))
            .GroupBy(v => v.YouTubeVideoId)
            .ToDictionary(g => g.Key, g => g.First());
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<VideoLesson>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken, int limit = 100)
    {
        IReadOnlyList<VideoLesson> result = _lessons.Values
            .Where(v => v.Status == IngestionStatus.Leveled
                        && Math.Abs((int)v.Level - (int)level) <= levelTolerance)
            .OrderBy(v => Math.Abs((int)v.Level - (int)level))
            .ThenByDescending(v => v.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(VideoLesson lesson, CancellationToken cancellationToken)
    {
        _lessons[lesson.Id] = lesson;
        return Task.CompletedTask;
    }
}
