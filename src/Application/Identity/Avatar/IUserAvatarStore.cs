namespace Application.Identity.Avatar;

public sealed record UserAvatarContent(
    byte[]? Data,
    string ContentType,
    DateTimeOffset UpdatedAt,
    string? ObjectKey = null,
    long? SizeBytes = null,
    string? Checksum = null,
    string? ETag = null,
    DateTimeOffset? StoredAt = null);

public interface IUserAvatarStore
{
    Task<UserAvatarContent?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveAsync(Guid userId, UserAvatarContent avatar, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken);
}
