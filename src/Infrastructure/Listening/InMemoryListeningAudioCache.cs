using System.Collections.Concurrent;
using Application.Listening.Ports;
using Infrastructure.Storage;

namespace Infrastructure.Listening;

public sealed class InMemoryListeningAudioCache : IListeningAudioCache
{
    private readonly ConcurrentDictionary<Guid, ListeningAudioContent> _audio = new();

    public Task<ListeningAudioContent?> GetAsync(Guid exerciseId, CancellationToken cancellationToken) =>
        Task.FromResult(_audio.TryGetValue(exerciseId, out var content) ? content : null);

    public Task SetAsync(Guid exerciseId, byte[] audio, string contentType, CancellationToken cancellationToken)
    {
        MediaValidation.EnsureSize(audio.LongLength, MediaValidation.MaxGeneratedAudioBytes);
        if (!MediaValidation.DetectAudio(audio).Equals(contentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Audio MIME type does not match its content.");
        _audio[exerciseId] = new ListeningAudioContent(audio, contentType, DateTimeOffset.UtcNow, SizeBytes: audio.LongLength);
        return Task.CompletedTask;
    }

    public Task<ListeningAudioMetadata?> GetMetadataAsync(Guid exerciseId, CancellationToken cancellationToken) =>
        Task.FromResult(_audio.TryGetValue(exerciseId, out var content)
            ? new ListeningAudioMetadata(content.SizeBytes ?? content.Audio?.LongLength ?? 0, content.CreatedAt, content.ContentType)
            : null);
}
