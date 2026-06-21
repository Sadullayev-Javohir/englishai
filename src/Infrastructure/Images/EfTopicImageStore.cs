using Application.Common;
using Application.Storage;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Images;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="ITopicImageStore"/>. Stores each topic's gallery of
/// thumbnails in a bytea-backed table keyed by (TopicId, Slot), so a downloaded licensed image
/// survives restarts and is re-served from the database, never re-fetched from the provider
/// (docs/development-guide.md rules 10, 12).
/// </summary>
public sealed class EfTopicImageStore : ITopicImageStore
{
    private readonly EnglishAiDbContext _db;
    private readonly IObjectStorage _storage;
    private readonly ObjectStorageOptions _options;

    public EfTopicImageStore(EnglishAiDbContext db, IObjectStorage storage, ObjectStorageOptions options)
    {
        _db = db;
        _storage = storage;
        _options = options;
    }

    public Task<TopicImageContent?> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken) =>
        GetBySlotAsync(topicId, 0, cancellationToken);

    public async Task<TopicImageContent?> GetBySlotAsync(Guid topicId, int slot, CancellationToken cancellationToken)
    {
        var row = await _db.TopicImages.AsNoTracking()
            .FirstOrDefaultAsync(i => i.TopicId == topicId && i.Slot == slot, cancellationToken);
        return row is null ? null : ToContent(row);
    }

    public async Task<IReadOnlyList<TopicImageSlotInfo>> GetManifestAsync(
        Guid topicId, CancellationToken cancellationToken) =>
        await _db.TopicImages.AsNoTracking()
            .Where(i => i.TopicId == topicId)
            .OrderBy(i => i.Slot)
            .Select(i => new TopicImageSlotInfo(
                i.Slot, i.Source, i.Attribution, i.SourceUrl, i.SafetyStatus))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetImageCountsAsync(CancellationToken cancellationToken)
    {
        var counts = await _db.TopicImages.AsNoTracking()
            .Where(image => image.SafetyStatus == ImageSafetyStatus.Safe)
            .GroupBy(i => i.TopicId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        return counts.ToDictionary(c => c.Key, c => c.Count);
    }

    public async Task SaveAsync(TopicImageContent image, CancellationToken cancellationToken)
    {
        if (image.Data is null) throw new InvalidDataException("Topic image content is required.");
        MediaValidation.EnsureSize(image.Data.LongLength, MediaValidation.MaxTopicImageBytes);
        var contentType = MediaValidation.DetectImage(image.Data);
        if (!contentType.Equals(image.ContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Topic image MIME type does not match its content.");
        var row = await _db.TopicImages
            .FirstOrDefaultAsync(i => i.TopicId == image.TopicId && i.Slot == image.Slot, cancellationToken);
        if (row is null)
        {
            row = new TopicImage { TopicId = image.TopicId, Slot = image.Slot };
            await _db.TopicImages.AddAsync(row, cancellationToken);
        }

        var previousKey = row.ObjectKey;
        if (!_options.Enabled)
        {
            row.Data = image.Data;
            row.ContentType = contentType;
            row.ObjectKey = null;
            row.SizeBytes = image.Data.LongLength;
            row.Checksum = ObjectKeys.Checksum(image.Data);
            row.ObjectETag = null;
            row.StoredAt = null;
            row.Source = image.Source;
            row.Attribution = image.Attribution;
            row.SourceUrl = image.SourceUrl;
            row.Width = image.Width;
            row.Height = image.Height;
            row.CreatedAt = image.CreatedAt;
            ApplySafety(row, image);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var checksum = ObjectKeys.Checksum(image.Data);
        var key = ObjectKeys.TopicImage(_options.KeyPrefix, image.TopicId, image.Slot, checksum, contentType);
        await using var stream = new MemoryStream(image.Data, writable: false);
        var stored = await _storage.PutAsync(new ObjectWriteRequest(
            key, stream, contentType, image.Data.LongLength, checksum, ObjectVisibility.Public,
            "public, max-age=31536000, immutable"), cancellationToken);

        row.Data = null;
        row.ContentType = contentType;
        row.ObjectKey = stored.Key;
        row.SizeBytes = stored.SizeBytes;
        row.Checksum = stored.Checksum;
        row.ObjectETag = stored.ETag;
        row.StoredAt = stored.StoredAt;
        row.Source = image.Source;
        row.Attribution = image.Attribution;
        row.SourceUrl = image.SourceUrl;
        row.Width = image.Width;
        row.Height = image.Height;
        row.CreatedAt = image.CreatedAt;
        ApplySafety(row, image);

        await _db.SaveChangesAsync(cancellationToken);
        // A vocabulary-image replacement updates this row and its word provenance in one database
        // transaction. Deleting the old object before that transaction commits would make a rollback
        // point at a missing object, so retain it for the storage lifecycle/cleanup job instead.
        if (_db.Database.CurrentTransaction is null
            && !string.IsNullOrWhiteSpace(previousKey)
            && !previousKey.Equals(stored.Key, StringComparison.Ordinal))
            await _storage.DeleteAsync(previousKey, ObjectVisibility.Public, cancellationToken);
    }

    public async Task MarkSafetyAsync(
        Guid topicId,
        int slot,
        ImageSafetyStatus status,
        string? modelVersion,
        DateTimeOffset checkedAt,
        string? reasons,
        CancellationToken cancellationToken)
    {
        var row = await _db.TopicImages
            .FirstOrDefaultAsync(image => image.TopicId == topicId && image.Slot == slot, cancellationToken);
        if (row is null)
            return;
        row.SafetyStatus = status;
        row.SafetyModelVersion = modelVersion;
        row.SafetyCheckedAt = checkedAt;
        row.SafetyReasons = reasons;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteAllAsync(CancellationToken cancellationToken) =>
        await _db.TopicImages.ExecuteDeleteAsync(cancellationToken);

    public async Task<int> DeleteAsync(Guid topicId, int slot, CancellationToken cancellationToken) =>
        await _db.TopicImages
            .Where(image => image.TopicId == topicId && image.Slot == slot)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task<ImageSafetyInventory> GetSafetyInventoryAsync(CancellationToken cancellationToken)
    {
        var counts = await _db.TopicImages.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new ImageSafetyInventory(
                group.Count(),
                group.Count(image => image.SafetyStatus == ImageSafetyStatus.Safe),
                group.Count(image => image.SafetyStatus == ImageSafetyStatus.Pending),
                group.Count(image => image.SafetyStatus == ImageSafetyStatus.Unsafe),
                group.Count(image => image.SafetyStatus == ImageSafetyStatus.Failed)))
            .SingleOrDefaultAsync(cancellationToken);
        return counts ?? new ImageSafetyInventory(0, 0, 0, 0, 0);
    }

    public async IAsyncEnumerable<TopicImageContent> GetAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Materialize first: the caller both reads AND writes (SaveAsync) on the same scoped
        // DbContext, and Npgsql rejects a second in-flight command while a reader is open. Pulling
        // the rows into memory up front avoids the "command already in progress" error on SaveAsync.
        var rows = await _db.TopicImages.AsNoTracking()
            .OrderBy(i => i.TopicId).ThenBy(i => i.Slot)
            .Select(i => new { i.TopicId, i.Slot })
            .ToListAsync(cancellationToken);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Fetch one payload at a time. Materializing every bytea value together can consume more
            // than a gigabyte in production, while yielding metadata-only rows makes maintenance
            // jobs such as TopicImageReencodeJob silently scan zero images.
            var image = await GetBySlotAsync(row.TopicId, row.Slot, cancellationToken);
            if (image is not null)
                yield return image;
        }
    }

    private static TopicImageContent ToContent(TopicImage row) => new(
        row.TopicId, row.Slot, row.Data, row.ContentType, row.Source, row.Attribution, row.SourceUrl,
        row.Width, row.Height, row.CreatedAt, row.ObjectKey, row.SizeBytes, row.Checksum,
        row.ObjectETag, row.StoredAt, row.SafetyStatus, row.SafetyModelVersion,
        row.SafetyCheckedAt, row.SafetyReasons);

    private static void ApplySafety(TopicImage row, TopicImageContent image)
    {
        row.SafetyStatus = image.SafetyStatus;
        row.SafetyModelVersion = image.SafetyModelVersion;
        row.SafetyCheckedAt = image.SafetyCheckedAt;
        row.SafetyReasons = image.SafetyReasons;
    }
}
