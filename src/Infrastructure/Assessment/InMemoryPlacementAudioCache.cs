using System.Collections.Concurrent;
using Application.Assessment.Ports;
using Infrastructure.Storage;

namespace Infrastructure.Assessment;

public sealed class InMemoryPlacementAudioCache : IPlacementAudioCache
{
    private readonly ConcurrentDictionary<Guid, PlacementAudioContent> _audio = new();

    public Task<PlacementAudioContent?> GetAsync(Guid questionId, CancellationToken cancellationToken) =>
        Task.FromResult(_audio.TryGetValue(questionId, out var content) ? content : null);

    public Task SetAsync(Guid questionId, byte[] audio, string contentType, CancellationToken cancellationToken)
    {
        MediaValidation.EnsureSize(audio.LongLength, MediaValidation.MaxGeneratedAudioBytes);
        if (!MediaValidation.DetectAudio(audio).Equals(contentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Audio MIME type does not match its content.");
        _audio[questionId] = new PlacementAudioContent(audio, contentType, DateTimeOffset.UtcNow, SizeBytes: audio.LongLength);
        return Task.CompletedTask;
    }
}
