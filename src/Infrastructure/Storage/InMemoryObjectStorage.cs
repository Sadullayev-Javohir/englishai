using System.Collections.Concurrent;
using Application.Storage;

namespace Infrastructure.Storage;

public sealed class InMemoryObjectStorage : IObjectStorage
{
    private readonly ConcurrentDictionary<(ObjectVisibility Visibility, string Key), Entry> _objects = new();

    public async Task<StoredObject> PutAsync(ObjectWriteRequest request, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(request.Key);
        await using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length != request.SizeBytes)
            throw new InvalidDataException("Object length does not match declared size.");
        var bytes = buffer.ToArray();
        if (!ObjectKeys.Checksum(bytes).Equals(request.Checksum, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Object checksum does not match content.");
        var metadata = new StoredObject(request.Key, request.ContentType, bytes.LongLength, request.Checksum,
            $"\"{request.Checksum}\"", DateTimeOffset.UtcNow, request.Visibility);
        _objects[(request.Visibility, request.Key)] = new Entry(bytes, metadata);
        return metadata;
    }

    public Task<StoredObject?> HeadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken) =>
        Task.FromResult(_objects.TryGetValue((visibility, key), out var entry) ? entry.Metadata : null);

    public Task<ObjectReadResult?> OpenReadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken) =>
        Task.FromResult(_objects.TryGetValue((visibility, key), out var entry)
            ? new ObjectReadResult(new MemoryStream(entry.Bytes, writable: false), entry.Metadata)
            : null);

    public Task DeleteAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken)
    {
        _objects.TryRemove((visibility, key), out _);
        return Task.CompletedTask;
    }

    public Uri GetPublicUrl(string key) => new($"https://storage.invalid/{Uri.EscapeDataString(key).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}");

    public Task<Uri> CreatePrivateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.invalid/private/{Uri.EscapeDataString(key).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}?expires={Math.Max(1, (int)lifetime.TotalSeconds)}"));

    private sealed record Entry(byte[] Bytes, StoredObject Metadata);
}
