using Application.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

public sealed class SafetyCheckedImageService : IImageService
{
    // Every external provider caps one search page at 30 candidates. Passing a larger value used to
    // reach provider code as Math.Clamp(..., count, 30), where count=32 made min greater than max and
    // threw before any replacement candidate could be downloaded.
    private const int MaxProviderCandidates = 30;

    private readonly IImageService _inner;
    private readonly IImageSafetyClassifier _classifier;
    private readonly ImageModerationOptions _options;
    private readonly ILogger<SafetyCheckedImageService> _logger;

    public SafetyCheckedImageService(
        IImageService inner,
        IImageSafetyClassifier classifier,
        ImageModerationOptions options,
        ILogger<SafetyCheckedImageService> logger)
    {
        _inner = inner;
        _classifier = classifier;
        _options = options;
        _logger = logger;
    }

    public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
        _options.RequireSafetyApproval
            ? Task.FromResult<ImageResult?>(null)
            : _inner.FindImageAsync(query, cancellationToken);

    public async Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken)
    {
        var candidates = await DownloadImagesAsync(query, 1, cancellationToken);
        return candidates.FirstOrDefault();
    }

    public async Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
        string query,
        int count,
        CancellationToken cancellationToken)
    {
        if (count <= 0)
            return [];

        var multiplier = Math.Max(1, _options.CandidateMultiplier);
        var requested = count >= MaxProviderCandidates
            ? MaxProviderCandidates
            : Math.Min(MaxProviderCandidates, count * multiplier);
        var candidates = await _inner.DownloadImagesAsync(query, requested, cancellationToken);
        var safe = new List<DownloadedImage>(count);
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var decision = await _classifier.ClassifyAsync(
                    candidate.Data, candidate.ContentType, cancellationToken);
                if (!decision.IsSafe)
                {
                    _logger.LogWarning(
                        "Rejected unsafe image candidate from {Source}; reasons {Reasons}.",
                        candidate.Source,
                        decision.ReasonSummary ?? "unspecified");
                    continue;
                }

                safe.Add(candidate with
                {
                    SafetyStatus = ImageSafetyStatus.Safe,
                    SafetyModelVersion = decision.ModelVersion,
                    SafetyCheckedAt = DateTimeOffset.UtcNow,
                    SafetyReasons = decision.ReasonSummary,
                });
                if (safe.Count >= count)
                    break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Image moderation failed closed for candidate from {Source}.", candidate.Source);
            }
        }

        return safe;
    }
}
