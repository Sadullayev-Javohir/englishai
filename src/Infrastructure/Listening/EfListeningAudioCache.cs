using Application.Listening.Ports;
using Application.Storage;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Listening;

public sealed class EfListeningAudioCache(
    EnglishAiDbContext db,
    IObjectStorage storage,
    ObjectStorageOptions options) : IListeningAudioCache
{
    public async Task<ListeningAudioContent?> GetAsync(Guid exerciseId, CancellationToken cancellationToken)
    {
        var clip = await db.ListeningAudioClips.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExerciseId == exerciseId, cancellationToken);
        if (clip is null) return null;
        return new ListeningAudioContent(clip.AudioContent, clip.ContentType, clip.CreatedAt,
            string.IsNullOrWhiteSpace(clip.ObjectKey) ? null : storage.GetPublicUrl(clip.ObjectKey).ToString(),
            clip.ObjectETag, clip.SizeBytes);
    }

    public async Task SetAsync(Guid exerciseId, byte[] audio, string contentType, CancellationToken cancellationToken)
    {
        MediaValidation.EnsureSize(audio.LongLength, MediaValidation.MaxGeneratedAudioBytes);
        var detectedType = MediaValidation.DetectAudio(audio);
        if (!detectedType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Audio MIME type does not match its content.");
        var row = await db.ListeningAudioClips.FirstOrDefaultAsync(x => x.ExerciseId == exerciseId, cancellationToken);
        var previousKey = row?.ObjectKey;
        if (row is null)
        {
            row = new ListeningAudioClip { ExerciseId = exerciseId };
            db.ListeningAudioClips.Add(row);
        }
        if (!options.Enabled)
        {
            row.AudioContent = audio;
            row.ContentType = detectedType;
            row.ObjectKey = null;
            row.SizeBytes = audio.LongLength;
            row.Checksum = ObjectKeys.Checksum(audio);
            row.ObjectETag = null;
            row.StoredAt = null;
            row.CreatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var checksum = ObjectKeys.Checksum(audio);
        var key = ObjectKeys.ListeningAudio(options.KeyPrefix, exerciseId, checksum, detectedType);
        await using var stream = new MemoryStream(audio, writable: false);
        var stored = await storage.PutAsync(new ObjectWriteRequest(
            key, stream, detectedType, audio.LongLength, checksum, ObjectVisibility.Public,
            "public, max-age=31536000, immutable"), cancellationToken);

        row.AudioContent = null;
        row.ContentType = detectedType;
        row.ObjectKey = stored.Key;
        row.SizeBytes = stored.SizeBytes;
        row.Checksum = stored.Checksum;
        row.ObjectETag = stored.ETag;
        row.StoredAt = stored.StoredAt;
        row.CreatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(previousKey) && !previousKey.Equals(stored.Key, StringComparison.Ordinal))
            await storage.DeleteAsync(previousKey, ObjectVisibility.Public, cancellationToken);
    }

    public async Task<ListeningAudioMetadata?> GetMetadataAsync(Guid exerciseId, CancellationToken cancellationToken)
    {
        var clip = await db.ListeningAudioClips.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExerciseId == exerciseId, cancellationToken);
        if (clip is null) return null;
        return new ListeningAudioMetadata(clip.SizeBytes ?? clip.AudioContent?.LongLength ?? 0, clip.CreatedAt, clip.ContentType);
    }
}
