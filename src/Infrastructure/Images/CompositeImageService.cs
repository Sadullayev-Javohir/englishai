using Application.Common;

namespace Infrastructure.Images;

/// <summary>
/// An <see cref="IImageService"/> that chains several providers (Unsplash first, then Pexels) so a
/// topic's gallery still fills if one provider rate-limits or has few matches (docs/development-guide.md rule 12).
/// Single-image lookups return the first provider that yields a result; gallery downloads accumulate
/// distinct images across providers until the requested count is reached. Each provider already
/// degrades gracefully (returns null/empty on any failure), so the chain never throws.
/// </summary>
public sealed class CompositeImageService : IImageService
{
    private readonly IReadOnlyList<IImageService> _providers;

    public CompositeImageService(IReadOnlyList<IImageService> providers)
    {
        _providers = providers;
    }

    public async Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            var result = await provider.FindImageAsync(query, cancellationToken);
            if (result is not null)
                return result;
        }

        return null;
    }

    public async Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            var result = await provider.DownloadImageAsync(query, cancellationToken);
            if (result is not null)
                return result;
        }

        return null;
    }

    public async Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
        string query, int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
            return Array.Empty<DownloadedImage>();

        var collected = new List<DownloadedImage>(count);
        // De-dupe across providers by source URL so the same photo isn't stored in two slots.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in _providers)
        {
            var images = await provider.DownloadImagesAsync(query, count, cancellationToken);
            foreach (var image in images)
            {
                if (image.SourceUrl is not null && !seen.Add(image.SourceUrl))
                    continue;
                collected.Add(image);
            }
        }

        return collected;
    }
}
