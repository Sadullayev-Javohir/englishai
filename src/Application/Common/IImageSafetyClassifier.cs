namespace Application.Common;

public enum ImageSafetyStatus
{
    Pending = 0,
    Safe = 1,
    Unsafe = 2,
    Failed = 3,
}

public sealed record ImageSafetyDecision(
    bool IsSafe,
    string ModelVersion,
    IReadOnlyList<string> Reasons)
{
    public string? ReasonSummary => Reasons.Count == 0 ? null : string.Join(',', Reasons);
}

public interface IImageSafetyClassifier
{
    Task<ImageSafetyDecision> ClassifyAsync(
        byte[] data,
        string contentType,
        CancellationToken cancellationToken);
}
