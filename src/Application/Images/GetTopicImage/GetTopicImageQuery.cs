using MediatR;

namespace Application.Images.GetTopicImage;

/// <summary>The served image bytes for a topic, with the MIME type to send them as.</summary>
public sealed record TopicImageResult(
    byte[]? Data,
    string ContentType,
    DateTimeOffset CreatedAt,
    string? PublicUrl = null,
    string? ETag = null,
    long? SizeBytes = null);

/// <summary>
/// Fetches one gallery image for a topic for serving (docs/development-guide.md rule 12 - the licensed image was
/// downloaded once and persisted; this re-serves it from the database). <see cref="Slot"/> selects
/// the gallery image (0 = cover); when that slot is empty it falls back to the cover so an
/// illustration place still shows something. Returns null when the topic has no image at all, so the
/// endpoint replies 404 and the UI falls back to a placeholder.
/// </summary>
public sealed record GetTopicImageQuery(Guid TopicId, int Slot = 0) : IRequest<TopicImageResult?>;
