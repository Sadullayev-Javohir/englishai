using System.Collections.Concurrent;
using Application.Assessment.Ports;
using Application.Storage;
using Infrastructure.Storage;

namespace Infrastructure.Assessment;

public sealed class ObjectPlacementAudioCache(
    IObjectStorage storage,
    ObjectStorageOptions options) : IPlacementAudioCache
{
    private readonly ConcurrentDictionary<Guid, PlacementAudioContent> _index = new();

    public Task<PlacementAudioContent?> GetAsync(Guid questionId, CancellationToken cancellationToken) =>
        Task.FromResult(_index.TryGetValue(questionId, out var content) ? content : null);

    public async Task SetAsync(Guid questionId, byte[] audio, string contentType, CancellationToken cancellationToken)
    {
        MediaValidation.EnsureSize(audio.LongLength, MediaValidation.MaxGeneratedAudioBytes);
        var detectedType = MediaValidation.DetectAudio(audio);
        if (!detectedType.Equals(contentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Audio MIME type does not match its content.");
        var checksum = ObjectKeys.Checksum(audio);
        var key = ObjectKeys.PlacementAudio(options.KeyPrefix, questionId, checksum, detectedType);
        await using var stream = new MemoryStream(audio, writable: false);
        var stored = await storage.PutAsync(new ObjectWriteRequest(
            key, stream, detectedType, audio.LongLength, checksum, ObjectVisibility.Public,
            "public, max-age=31536000, immutable"), cancellationToken);
        _index[questionId] = new PlacementAudioContent(null, detectedType, stored.StoredAt,
            storage.GetPublicUrl(key).ToString(), stored.ETag, stored.SizeBytes);
    }
}
