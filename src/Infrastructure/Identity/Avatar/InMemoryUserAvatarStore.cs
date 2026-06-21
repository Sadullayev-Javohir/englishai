using System.Collections.Concurrent;
using Application.Identity.Avatar;
using Infrastructure.Storage;

namespace Infrastructure.Identity.Avatar;

public sealed class InMemoryUserAvatarStore : IUserAvatarStore
{
    private readonly ConcurrentDictionary<Guid, UserAvatarContent> _avatars = new();

    public Task<UserAvatarContent?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_avatars.TryGetValue(userId, out var avatar) ? avatar : null);

    public Task SaveAsync(Guid userId, UserAvatarContent avatar, CancellationToken cancellationToken)
    {
        if (avatar.Data is null) throw new InvalidDataException("Avatar content is required.");
        MediaValidation.EnsureSize(avatar.Data.LongLength, MediaValidation.MaxAvatarBytes);
        if (!MediaValidation.DetectImage(avatar.Data).Equals(avatar.ContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Avatar MIME type does not match its content.");
        _avatars[userId] = avatar;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        _avatars.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}
