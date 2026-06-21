using Application.Common;

namespace Infrastructure.Images;

/// <summary>
/// No-op <see cref="IImageService"/> used when no image-provider key is configured. It returns
/// null for every query so callers degrade gracefully - the Books UI renders a generated gradient
/// placeholder cover instead of a fetched photo. This keeps the app runnable and copyright-safe
/// without any external image API (docs/development-guide.md rule 12).
/// </summary>
public sealed class LocalImageService : IImageService
{
    public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult<ImageResult?>(null);

    public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken) =>
        Task.FromResult<DownloadedImage?>(null);

    public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
        string query, int count, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DownloadedImage>>(Array.Empty<DownloadedImage>());
}
