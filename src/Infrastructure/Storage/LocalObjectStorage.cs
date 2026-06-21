using Application.Storage;

namespace Infrastructure.Storage;

public sealed class LocalObjectStorage : IObjectStorage
{
    private readonly string root;

    public LocalObjectStorage(ObjectStorageOptions options)
    {
        root = Path.GetFullPath(options.LocalPath);
        Directory.CreateDirectory(root);
    }

    public async Task<StoredObject> PutAsync(ObjectWriteRequest request, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(request.Key);
        var path = Resolve(request.Key, request.Visibility);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            await request.Content.CopyToAsync(output, cancellationToken);

        var info = new FileInfo(temporaryPath);
        if (info.Length != request.SizeBytes)
        {
            File.Delete(temporaryPath);
            throw new InvalidDataException("Object length does not match declared size.");
        }

        File.Move(temporaryPath, path, true);
        var metadata = new StoredObject(request.Key, request.ContentType, request.SizeBytes, request.Checksum,
            $"\"{request.Checksum}\"", DateTimeOffset.UtcNow, request.Visibility);
        await File.WriteAllTextAsync(MetadataPath(path), System.Text.Json.JsonSerializer.Serialize(metadata), cancellationToken);
        return metadata;
    }

    public async Task<StoredObject?> HeadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(key);
        var path = Resolve(key, visibility);
        if (!File.Exists(path)) return null;
        var metadataPath = MetadataPath(path);
        if (File.Exists(metadataPath))
            return System.Text.Json.JsonSerializer.Deserialize<StoredObject>(await File.ReadAllTextAsync(metadataPath, cancellationToken));
        var info = new FileInfo(path);
        return new StoredObject(key, "application/octet-stream", info.Length, string.Empty, null, info.LastWriteTimeUtc, visibility);
    }

    public async Task<ObjectReadResult?> OpenReadAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken)
    {
        var metadata = await HeadAsync(key, visibility, cancellationToken);
        if (metadata is null) return null;
        return new ObjectReadResult(new FileStream(Resolve(key, visibility), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true), metadata);
    }

    public Task DeleteAsync(string key, ObjectVisibility visibility, CancellationToken cancellationToken)
    {
        ObjectKeys.Validate(key);
        var path = Resolve(key, visibility);
        File.Delete(path);
        File.Delete(MetadataPath(path));
        return Task.CompletedTask;
    }

    public Uri GetPublicUrl(string key) => new($"https://storage.invalid/{Uri.EscapeDataString(key).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}");

    public Task<Uri> CreatePrivateReadUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken) =>
        Task.FromResult(new Uri($"https://storage.invalid/private/{Uri.EscapeDataString(key).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase)}?expires={Math.Max(1, (int)lifetime.TotalSeconds)}"));

    private string Resolve(string key, ObjectVisibility visibility)
    {
        var path = Path.GetFullPath(Path.Combine(root, visibility.ToString().ToLowerInvariant(), key.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Object key resolves outside storage root.");
        return path;
    }

    private static string MetadataPath(string path) => $"{path}.metadata.json";
}
