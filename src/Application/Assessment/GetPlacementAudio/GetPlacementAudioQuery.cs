using MediatR;

namespace Application.Assessment.GetPlacementAudio;

/// <summary>
/// Returns the spoken audio for a listening item so the learner hears the clip
/// instead of reading it. The script text itself is never returned to the client.
/// </summary>
public sealed record GetPlacementAudioQuery(Guid QuestionId) : IRequest<PlacementAudioResult>;

/// <summary>The synthesized clip and the MIME type the browser should play it as.</summary>
public sealed record PlacementAudioResult(
    byte[]? Audio,
    string ContentType,
    DateTimeOffset CreatedAt,
    string? PublicUrl = null,
    string? ETag = null,
    long? SizeBytes = null);
