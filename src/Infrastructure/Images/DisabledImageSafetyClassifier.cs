using Application.Common;

namespace Infrastructure.Images;

public sealed class DisabledImageSafetyClassifier : IImageSafetyClassifier
{
    public Task<ImageSafetyDecision> ClassifyAsync(
        byte[] data,
        string contentType,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ImageSafetyDecision(true, "disabled", []));
}
