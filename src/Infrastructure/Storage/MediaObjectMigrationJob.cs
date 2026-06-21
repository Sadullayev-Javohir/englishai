using Application.Storage;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Storage;

public sealed class MediaObjectMigrationJob(
    EnglishAiDbContext db,
    IObjectStorage storage,
    ObjectStorageOptions options,
    ILogger<MediaObjectMigrationJob> logger)
{
    public sealed record Result(int Scanned, int Migrated, int Skipped);

    public async Task<Result> RunAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Media object migration skipped because object storage is disabled.");
            return new Result(0, 0, 0);
        }

        batchSize = Math.Clamp(batchSize, 1, 500);
        var scanned = 0;
        var migrated = 0;
        var skipped = 0;

        var avatars = await db.UserAvatars.Where(x => x.Data != null).OrderBy(x => x.UserId).Take(batchSize).ToListAsync(cancellationToken);
        foreach (var row in avatars)
        {
            scanned++;
            var data = row.Data!;
            var contentType = MediaValidation.DetectImage(data);
            var checksum = ObjectKeys.Checksum(data);
            var key = ObjectKeys.Avatar(options.KeyPrefix, row.UserId, checksum);
            var stored = await EnsureStoredAsync(key, data, contentType, checksum, ObjectVisibility.Private, "private, no-store", cancellationToken);
            row.Data = null;
            Apply(row, stored);
            migrated++;
        }
        await db.SaveChangesAsync(cancellationToken);

        var remaining = Math.Max(0, batchSize - avatars.Count);
        var images = await db.TopicImages.Where(x => x.Data != null).OrderBy(x => x.TopicId).ThenBy(x => x.Slot).Take(remaining).ToListAsync(cancellationToken);
        foreach (var row in images)
        {
            scanned++;
            var data = row.Data!;
            var contentType = MediaValidation.DetectImage(data);
            var checksum = ObjectKeys.Checksum(data);
            var key = ObjectKeys.TopicImage(options.KeyPrefix, row.TopicId, row.Slot, checksum, contentType);
            var stored = await EnsureStoredAsync(key, data, contentType, checksum, ObjectVisibility.Public, "public, max-age=31536000, immutable", cancellationToken);
            row.Data = null;
            Apply(row, stored);
            migrated++;
        }
        await db.SaveChangesAsync(cancellationToken);

        remaining = Math.Max(0, remaining - images.Count);
        var audio = await db.ListeningAudioClips.Where(x => x.AudioContent != null).OrderBy(x => x.ExerciseId).Take(remaining).ToListAsync(cancellationToken);
        foreach (var row in audio)
        {
            scanned++;
            var data = row.AudioContent!;
            var contentType = MediaValidation.DetectAudio(data);
            var checksum = ObjectKeys.Checksum(data);
            var key = ObjectKeys.ListeningAudio(options.KeyPrefix, row.ExerciseId, checksum, contentType);
            var stored = await EnsureStoredAsync(key, data, contentType, checksum, ObjectVisibility.Public, "public, max-age=31536000, immutable", cancellationToken);
            row.AudioContent = null;
            row.ContentType = contentType;
            Apply(row, stored);
            migrated++;
        }
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Media object migration scanned {Scanned}, migrated {Migrated}, skipped {Skipped}.", scanned, migrated, skipped);
        return new Result(scanned, migrated, skipped);
    }

    private async Task<StoredObject> EnsureStoredAsync(
        string key, byte[] data, string contentType, string checksum, ObjectVisibility visibility,
        string cacheControl, CancellationToken cancellationToken)
    {
        var existing = await storage.HeadAsync(key, visibility, cancellationToken);
        if (existing is not null && existing.SizeBytes == data.LongLength
            && existing.Checksum.Equals(checksum, StringComparison.OrdinalIgnoreCase))
            return existing;
        await using var stream = new MemoryStream(data, writable: false);
        return await storage.PutAsync(new ObjectWriteRequest(key, stream, contentType, data.LongLength,
            checksum, visibility, cacheControl), cancellationToken);
    }

    private static void Apply(Identity.Avatar.UserAvatar row, StoredObject stored)
    {
        row.ObjectKey = stored.Key; row.SizeBytes = stored.SizeBytes; row.Checksum = stored.Checksum;
        row.ObjectETag = stored.ETag; row.StoredAt = stored.StoredAt;
    }

    private static void Apply(Images.TopicImage row, StoredObject stored)
    {
        row.ObjectKey = stored.Key; row.SizeBytes = stored.SizeBytes; row.Checksum = stored.Checksum;
        row.ObjectETag = stored.ETag; row.StoredAt = stored.StoredAt;
    }

    private static void Apply(Listening.ListeningAudioClip row, StoredObject stored)
    {
        row.ObjectKey = stored.Key; row.SizeBytes = stored.SizeBytes; row.Checksum = stored.Checksum;
        row.ObjectETag = stored.ETag; row.StoredAt = stored.StoredAt;
    }
}
