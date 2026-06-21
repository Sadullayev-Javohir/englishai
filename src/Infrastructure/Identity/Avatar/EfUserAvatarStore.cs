using Application.Identity.Avatar;
using Application.Storage;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Avatar;

public sealed class EfUserAvatarStore(
    EnglishAiDbContext db,
    IObjectStorage storage,
    ObjectStorageOptions options) : IUserAvatarStore
{
    public async Task<UserAvatarContent?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var avatar = await db.UserAvatars.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        return avatar is null ? null : ToContent(avatar);
    }

    public async Task SaveAsync(Guid userId, UserAvatarContent avatar, CancellationToken cancellationToken)
    {
        if (avatar.Data is null) throw new InvalidDataException("Avatar content is required.");
        MediaValidation.EnsureSize(avatar.Data.LongLength, MediaValidation.MaxAvatarBytes);
        var detectedType = MediaValidation.DetectImage(avatar.Data);
        if (!detectedType.Equals(avatar.ContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Avatar MIME type does not match its content.");

        var row = await db.UserAvatars.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        var previousKey = row?.ObjectKey;
        if (row is null)
        {
            row = new UserAvatar { UserId = userId };
            db.UserAvatars.Add(row);
        }
        if (!options.Enabled)
        {
            row.Data = avatar.Data;
            row.ContentType = detectedType;
            row.ObjectKey = null;
            row.SizeBytes = avatar.Data.LongLength;
            row.Checksum = ObjectKeys.Checksum(avatar.Data);
            row.ObjectETag = null;
            row.StoredAt = null;
            row.UpdatedAt = avatar.UpdatedAt;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var checksum = ObjectKeys.Checksum(avatar.Data);
        var key = ObjectKeys.Avatar(options.KeyPrefix, userId, checksum);
        await using var stream = new MemoryStream(avatar.Data, writable: false);
        var stored = await storage.PutAsync(new ObjectWriteRequest(
            key, stream, detectedType, avatar.Data.LongLength, checksum, ObjectVisibility.Private,
            "private, no-store"), cancellationToken);

        row.Data = null;
        row.ContentType = detectedType;
        row.ObjectKey = stored.Key;
        row.SizeBytes = stored.SizeBytes;
        row.Checksum = stored.Checksum;
        row.ObjectETag = stored.ETag;
        row.StoredAt = stored.StoredAt;
        row.UpdatedAt = avatar.UpdatedAt;
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(previousKey) && !previousKey.Equals(stored.Key, StringComparison.Ordinal))
            await storage.DeleteAsync(previousKey, ObjectVisibility.Private, cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await db.UserAvatars.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (row is null) return;
        db.UserAvatars.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(row.ObjectKey))
            await storage.DeleteAsync(row.ObjectKey, ObjectVisibility.Private, cancellationToken);
    }

    private static UserAvatarContent ToContent(UserAvatar row) => new(
        row.Data, row.ContentType, row.UpdatedAt, row.ObjectKey, row.SizeBytes,
        row.Checksum, row.ObjectETag, row.StoredAt);
}
