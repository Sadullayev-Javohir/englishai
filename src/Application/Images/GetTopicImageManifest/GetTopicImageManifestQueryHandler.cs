using Application.Common;
using MediatR;

namespace Application.Images.GetTopicImageManifest;

/// <summary>
/// Builds a topic's gallery manifest from the image store (docs/development-guide.md rule 12). Returns an empty
/// manifest when no images have been downloaded for the topic, so the UI simply renders no gallery.
/// </summary>
public sealed class GetTopicImageManifestQueryHandler
    : IRequestHandler<GetTopicImageManifestQuery, TopicImageManifestDto>
{
    private readonly ITopicImageStore _store;
    private readonly bool _requireSafetyApproval;

    public GetTopicImageManifestQueryHandler(ITopicImageStore store, ImageSafetyServingOptions? options = null)
    {
        _store = store;
        _requireSafetyApproval = options?.RequireSafetyApproval ?? false;
    }

    public async Task<TopicImageManifestDto> Handle(
        GetTopicImageManifestQuery request, CancellationToken cancellationToken)
    {
        var slots = await _store.GetManifestAsync(request.TopicId, cancellationToken);
        var images = slots
            .Where(slot => !_requireSafetyApproval || slot.SafetyStatus == ImageSafetyStatus.Safe)
            .Select(s => new TopicImageManifestEntry(s.Slot, s.Source, s.Attribution, s.SourceUrl))
            .ToList();
        return new TopicImageManifestDto(request.TopicId, images);
    }
}
