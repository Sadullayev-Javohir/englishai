using MediatR;

namespace Application.Images.GetTopicImageManifest;

/// <summary>One image in a topic's gallery for the frontend manifest (no bytes - just the slot to
/// request and its attribution, rule 12).</summary>
public sealed record TopicImageManifestEntry(int Slot, string Source, string? Attribution, string? SourceUrl);

/// <summary>A topic's gallery manifest: every slot that has a downloaded image, in slot order.</summary>
public sealed record TopicImageManifestDto(Guid TopicId, IReadOnlyList<TopicImageManifestEntry> Images);

/// <summary>
/// Lists the slots in a topic's image gallery (without bytes) so the frontend knows how many images
/// exist and how to attribute each, then requests each by slot from the image endpoint (docs/development-guide.md
/// rules 10, 12). Always returns a manifest (possibly empty) so the caller renders a gallery or
/// nothing.
/// </summary>
public sealed record GetTopicImageManifestQuery(Guid TopicId) : IRequest<TopicImageManifestDto>;
