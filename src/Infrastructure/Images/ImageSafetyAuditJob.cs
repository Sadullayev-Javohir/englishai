using Application.Common;
using Infrastructure.Jobs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

public sealed record ImageSafetyAuditResult(
    int Scanned,
    int Approved,
    int RemovedUnsafe,
    int RemovedFailed,
    int ForcedReplacements);

public sealed class ImageSafetyAuditJob
{
    private const int BatchSize = 100;
    private static readonly IReadOnlySet<(Guid Id, int Slot)> ForcedImageReplacements =
        new HashSet<(Guid, int)>
        {
            (Guid.Parse("cbf52b41-e97f-4dff-96af-872b36ab7f5f"), 3),
        };

    private readonly ITopicImageStore _store;
    private readonly IImageSafetyClassifier _classifier;
    private readonly ILogger<ImageSafetyAuditJob> _logger;
    private readonly IBackgroundJobScheduler? _jobs;

    public ImageSafetyAuditJob(
        ITopicImageStore store,
        IImageSafetyClassifier classifier,
        ILogger<ImageSafetyAuditJob> logger,
        IBackgroundJobScheduler? jobs = null)
    {
        _store = store;
        _classifier = classifier;
        _logger = logger;
        _jobs = jobs;
    }

    public async Task<ImageSafetyAuditResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var imageKeys = new List<(Guid TopicId, int Slot)>();
        await foreach (var image in _store.GetAllAsync(cancellationToken))
        {
            if (image.SafetyStatus != ImageSafetyStatus.Safe
                || ForcedImageReplacements.Contains((image.TopicId, image.Slot)))
                imageKeys.Add((image.TopicId, image.Slot));
            if (imageKeys.Count >= BatchSize)
                break;
        }

        var approved = 0;
        var removedUnsafe = 0;
        var removedFailed = 0;
        var forced = 0;
        foreach (var imageKey in imageKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ForcedImageReplacements.Contains(imageKey))
            {
                await _store.DeleteAsync(imageKey.TopicId, imageKey.Slot, cancellationToken);
                forced++;
                continue;
            }

            var image = await _store.GetBySlotAsync(imageKey.TopicId, imageKey.Slot, cancellationToken);
            if (image is null)
                continue;

            if (image.Data is null)
            {
                await _store.DeleteAsync(image.TopicId, image.Slot, cancellationToken);
                removedFailed++;
                continue;
            }

            try
            {
                var decision = await _classifier.ClassifyAsync(
                    image.Data, image.ContentType, cancellationToken);
                if (!decision.IsSafe)
                {
                    await _store.DeleteAsync(image.TopicId, image.Slot, cancellationToken);
                    removedUnsafe++;
                    _logger.LogWarning(
                        "Removed unsafe stored image {ImageId}/{Slot}; reasons {Reasons}.",
                        image.TopicId,
                        image.Slot,
                        decision.ReasonSummary ?? "unspecified");
                    continue;
                }

                await _store.MarkSafetyAsync(
                    image.TopicId,
                    image.Slot,
                    ImageSafetyStatus.Safe,
                    decision.ModelVersion,
                    DateTimeOffset.UtcNow,
                    decision.ReasonSummary,
                    cancellationToken);
                approved++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                await _store.DeleteAsync(image.TopicId, image.Slot, cancellationToken);
                removedFailed++;
                _logger.LogWarning(
                    ex,
                    "Image audit failed closed and removed {ImageId}/{Slot}.",
                    image.TopicId,
                    image.Slot);
            }
        }

        _logger.LogInformation(
            "Image safety audit scanned {Scanned}; approved {Approved}; removed unsafe {Unsafe}; removed failed {Failed}; forced replacements {Forced}.",
            imageKeys.Count,
            approved,
            removedUnsafe,
            removedFailed,
            forced);
        if (imageKeys.Count == BatchSize && _jobs is not null)
            _jobs.EnqueueContent<ImageSafetyAuditJob>(job => job.RunAsync(CancellationToken.None));
        return new ImageSafetyAuditResult(imageKeys.Count, approved, removedUnsafe, removedFailed, forced);
    }
}
