namespace Application.Storage;

public enum ObjectVisibility
{
    Public,
    Private,
}

public sealed record StoredObject(
    string Key,
    string ContentType,
    long SizeBytes,
    string Checksum,
    string? ETag,
    DateTimeOffset StoredAt,
    ObjectVisibility Visibility);

public sealed record ObjectWriteRequest(
    string Key,
    Stream Content,
    string ContentType,
    long SizeBytes,
    string Checksum,
    ObjectVisibility Visibility,
    string CacheControl);

public sealed record ObjectReadResult(Stream Content, StoredObject Metadata) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

public interface IObjectStorage
{
    Task<StoredObject> PutAsync(ObjectWriteRequest request, CancellationToken cancellationToken);
    Task<StoredObject?> HeadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken);
    Task<ObjectReadResult?> OpenReadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken);
    Task DeleteAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken);
    Uri GetPublicUrl(string key);
    Task<Uri> CreatePrivateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken);
}

public sealed class ObjectStorageUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
