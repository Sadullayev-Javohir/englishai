namespace Application.Assessment.Ports;

/// <summary>
/// Caches synthesized listening-clip audio keyed by question id. A clip is identical
/// for every learner, so it is synthesized once and reused (docs/development-guide.md rule 10: cache
/// repeated TTS phrases to avoid repeated Azure cost).
/// </summary>
public interface IPlacementAudioCache
{
    Task<PlacementAudioContent?> GetAsync(Guid questionId, CancellationToken cancellationToken);

    Task SetAsync(Guid questionId, byte[] audio, string contentType, CancellationToken cancellationToken);
}

public sealed record PlacementAudioContent(
    byte[]? Audio,
    string ContentType,
    DateTimeOffset CreatedAt,
    string? PublicUrl = null,
    string? ETag = null,
    long? SizeBytes = null);
