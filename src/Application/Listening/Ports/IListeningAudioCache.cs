namespace Application.Listening.Ports;

/// <summary>
/// Caches synthesized listening-clip audio keyed by exercise id. A clip is identical for
/// every learner, so it is synthesized once (Azure Neural TTS) and reused (docs/development-guide.md rule 10:
/// cache repeated TTS phrases to avoid repeated Azure cost).
/// </summary>
public interface IListeningAudioCache
{
    Task<ListeningAudioContent?> GetAsync(Guid exerciseId, CancellationToken cancellationToken);

    Task SetAsync(Guid exerciseId, byte[] audio, string contentType, CancellationToken cancellationToken);

    Task<ListeningAudioMetadata?> GetMetadataAsync(Guid exerciseId, CancellationToken cancellationToken);
}

public sealed record ListeningAudioMetadata(long SizeBytes, DateTimeOffset CreatedAt, string ContentType);
public sealed record ListeningAudioContent(
    byte[]? Audio,
    string ContentType,
    DateTimeOffset CreatedAt,
    string? PublicUrl = null,
    string? ETag = null,
    long? SizeBytes = null);
