using Application.Common;
using MediatR;

namespace Application.Images.GetTopicImage;

/// <summary>
/// Serves a topic's stored thumbnail from the database (docs/development-guide.md rules 10, 12 - fetched once,
/// re-served from storage). Returns null when no image has been downloaded for the topic so the
/// endpoint can reply 404 and the UI shows a placeholder.
/// </summary>
public sealed class GetTopicImageQueryHandler : IRequestHandler<GetTopicImageQuery, TopicImageResult?>
{
    private readonly ITopicImageStore _store;
    private readonly bool _requireSafetyApproval;

    public GetTopicImageQueryHandler(ITopicImageStore store, ImageSafetyServingOptions? options = null)
    {
        _store = store;
        _requireSafetyApproval = options?.RequireSafetyApproval ?? false;
    }

    public async Task<TopicImageResult?> Handle(GetTopicImageQuery request, CancellationToken cancellationToken)
    {
        var image = await _store.GetBySlotAsync(request.TopicId, request.Slot, cancellationToken);

        // Fall back to the cover (slot 0) when a requested illustration slot is empty, so a hero or
        // gallery place still shows something rather than a placeholder.
        if (image is null && request.Slot != 0)
            image = await _store.GetByTopicIdAsync(request.TopicId, cancellationToken);

        if (image is null || (_requireSafetyApproval && image.SafetyStatus != ImageSafetyStatus.Safe))
            return null;
        var publicUrl = image.ObjectKey is null ? null : $"object:{image.ObjectKey}";
        return new TopicImageResult(image.Data, image.ContentType, image.CreatedAt, publicUrl, image.ETag, image.SizeBytes);
    }
}
