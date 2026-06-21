using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Video;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IVideoRepository"/>. The owned transcript
/// and question collections load and persist with each lesson.
/// </summary>
public sealed class EfVideoRepository : IVideoRepository
{
    private readonly EnglishAiDbContext _db;

    public EfVideoRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task<VideoLesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.VideoLessons.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<VideoLesson?> GetByYouTubeIdAsync(string youTubeVideoId, CancellationToken cancellationToken) =>
        await _db.VideoLessons.FirstOrDefaultAsync(v => v.YouTubeVideoId == youTubeVideoId, cancellationToken);

    public async Task<IReadOnlyDictionary<string, VideoLesson>> GetByYouTubeIdsAsync(
        IReadOnlyCollection<string> youTubeVideoIds, CancellationToken cancellationToken)
    {
        if (youTubeVideoIds.Count == 0)
            return new Dictionary<string, VideoLesson>();

        var lessons = await _db.VideoLessons
            .AsNoTracking()
            .AsSplitQuery()
            .Where(v => youTubeVideoIds.Contains(v.YouTubeVideoId))
            .ToListAsync(cancellationToken);

        // De-dupe defensively: keep the first lesson per YouTube id.
        return lessons
            .GroupBy(v => v.YouTubeVideoId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<IReadOnlyList<VideoLesson>> GetCatalogForLevelAsync(
        CefrLevel level, int levelTolerance, CancellationToken cancellationToken, int limit = 100)
    {
        var lower = level - levelTolerance;
        var upper = level + levelTolerance;

        var lessons = await _db.VideoLessons
            .AsNoTracking()
            .AsSplitQuery()
            .Where(v => v.Status == IngestionStatus.Leveled && v.Level >= lower && v.Level <= upper)
            .OrderBy(v => v.Level == level ? 0 : v.Level < level ? level - v.Level : v.Level - level)
            .ThenByDescending(v => v.CreatedAt)
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync(cancellationToken);
        return lessons;
    }

    public async Task SaveAsync(VideoLesson lesson, CancellationToken cancellationToken)
    {
        if (_db.Entry(lesson).State == EntityState.Detached)
            await _db.VideoLessons.AddAsync(lesson, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
