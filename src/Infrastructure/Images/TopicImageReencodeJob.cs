using Application.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

/// <summary>
/// One-run operator job that re-encodes every stored topic image to compact WebP in place,
/// without re-downloading from the provider (rule 12 - the licensed bytes already live in the
/// database; we only compact them). This is the remediation for a gallery that was seeded as
/// large JPEG/PNG before WebP storage was enabled. It is idempotent: images already WebP (or
/// that do not shrink) are left untouched, so it is safe to re-run.
///
/// Triggered via POST /api/admin/reencode-images (Hangfire, off the request path), like the
/// other operator backfills.
/// </summary>
public sealed class TopicImageReencodeJob
{
    private readonly ITopicImageStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<TopicImageReencodeJob> _logger;

    public TopicImageReencodeJob(
        ITopicImageStore store, TimeProvider clock, ILogger<TopicImageReencodeJob> logger)
    {
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    public sealed record TopicImageReencodeResult(int Scanned, int Reencoded, long BytesBefore, long BytesAfter);

    public async Task<TopicImageReencodeResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var scanned = 0;
        var reencoded = 0;
        long bytesBefore = 0;
        long bytesAfter = 0;

        await foreach (var image in _store.GetAllAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (image.Data is null)
                continue;
            scanned++;
            bytesBefore += image.Data.Length;

            var optimized = WebpConverter.ToWebp(image.Data, image.ContentType);

            // Only write back when the bytes actually changed (new webp, smaller, different type).
            if (!string.Equals(optimized.ContentType, image.ContentType, StringComparison.OrdinalIgnoreCase)
                || optimized.Data.Length != image.Data.Length)
            {
                await _store.SaveAsync(
                    new TopicImageContent(
                        image.TopicId, image.Slot, optimized.Data, optimized.ContentType,
                        image.Source, image.Attribution, image.SourceUrl,
                        image.Width, image.Height, image.CreatedAt,
                        SafetyStatus: image.SafetyStatus,
                        SafetyModelVersion: image.SafetyModelVersion,
                        SafetyCheckedAt: image.SafetyCheckedAt,
                        SafetyReasons: image.SafetyReasons),
                    cancellationToken);
                reencoded++;
                bytesAfter += optimized.Data.Length;
            }
            else
            {
                bytesAfter += image.Data.Length;
            }
        }

        _logger.LogInformation(
            "Topic image re-encode: scanned {Scanned}, re-encoded {Reencoded} to WebP. " +
            "Bytes {Before} -> {After} (saved {Saved}).",
            scanned, reencoded, bytesBefore, bytesAfter, bytesBefore - bytesAfter);

        return new TopicImageReencodeResult(scanned, reencoded, bytesBefore, bytesAfter);
    }
}
