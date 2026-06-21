using System.Text.Json;
using Application.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

/// <summary>
/// <see cref="IImageService"/> backed by the Pexels API. Used as the second provider in the topic
/// gallery (after Unsplash) so a gallery still fills if Unsplash rate-limits or has few matches
/// (docs/development-guide.md rule 12 - licensed source, attribution preserved). <see cref="FindImageAsync"/>
/// returns a hot-link URL; the download methods fetch the bytes so the caller can persist them and
/// re-serve from the database. Any failure (network, rate limit, no match) returns null/empty so
/// callers degrade gracefully - never a blocking error.
/// </summary>
public sealed class PexelsImageService : IImageService
{
    private const string SearchEndpoint = "https://api.pexels.com/v1/search";

    private readonly HttpClient _http;
    private readonly ImageOptions _options;
    private readonly ILogger<PexelsImageService> _logger;

    public PexelsImageService(HttpClient http, ImageOptions options, ILogger<PexelsImageService> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken)
    {
        var photos = await SearchPhotosAsync(query, "portrait", 1, cancellationToken);
        return photos.Count == 0 ? null : new ImageResult(photos[0].DownloadUrl, photos[0].Attribution);
    }

    public async Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken)
    {
        var images = await DownloadImagesAsync(query, 1, cancellationToken);
        return images.Count > 0 ? images[0] : null;
    }

    public async Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
        string query, int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
            return Array.Empty<DownloadedImage>();

        var photos = await SearchPhotosAsync(query, "landscape", Math.Clamp(count * 3, count, 30), cancellationToken);
        if (photos.Count == 0)
            return Array.Empty<DownloadedImage>();

        var downloaded = new List<DownloadedImage>(photos.Count);
        foreach (var photo in photos)
        {
            var image = await DownloadOneAsync(photo, query, cancellationToken);
            if (image is not null)
                downloaded.Add(image);
            if (downloaded.Count >= count)
                break;
        }

        return downloaded;
    }

    private async Task<DownloadedImage?> DownloadOneAsync(
        PexelsPhoto photo, string query, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(photo.DownloadUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Pexels image download for '{Query}' returned {Status}.", query, response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return new DownloadedImage(
                bytes, contentType, "Pexels", photo.Attribution, photo.SourceUrl, photo.Width, photo.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pexels image download for '{Query}' failed.", query);
            return null;
        }
    }

    private async Task<IReadOnlyList<PexelsPhoto>> SearchPhotosAsync(
        string query, string orientation, int count, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(_options.PexelsApiKey))
            return Array.Empty<PexelsPhoto>();

        try
        {
            var perPage = Math.Clamp(count * 3, 1, 80);
            var url = $"{SearchEndpoint}?query={Uri.EscapeDataString(query)}&per_page={perPage}" +
                      $"&orientation={orientation}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", _options.PexelsApiKey);

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Pexels lookup for '{Query}' returned {Status}.", query, response.StatusCode);
                return Array.Empty<PexelsPhoto>();
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("photos", out var photos) ||
                photos.ValueKind != JsonValueKind.Array)
                return Array.Empty<PexelsPhoto>();

            var results = new List<PexelsPhoto>();
            foreach (var item in photos.EnumerateArray())
            {
                var photo = ParsePhoto(item);
                if (photo is not null)
                    results.Add(photo);
                if (results.Count >= count)
                    break;
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pexels lookup for '{Query}' failed.", query);
            return Array.Empty<PexelsPhoto>();
        }
    }

    private static PexelsPhoto? ParsePhoto(JsonElement item)
    {
        // "src.large" is a sensibly sized rendition; fall back to the original if absent.
        var imageUrl = item.TryGetProperty("src", out var src)
            ? (src.TryGetProperty("large", out var large) ? large.GetString()
                : src.TryGetProperty("original", out var original) ? original.GetString() : null)
            : null;
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var alt = item.TryGetProperty("alt", out var altEl) ? altEl.GetString() : null;
        var author = item.TryGetProperty("photographer", out var p) ? p.GetString() : null;
        if (!ImageSafetyFilter.IsSafe(alt, author))
            return null;

        var attribution = string.IsNullOrWhiteSpace(author)
            ? "Photo via Pexels"
            : $"Photo by {author} on Pexels";

        var sourceUrl = item.TryGetProperty("url", out var u) ? u.GetString() : null;
        var width = item.TryGetProperty("width", out var w) && w.TryGetInt32(out var wv) ? wv : 0;
        var height = item.TryGetProperty("height", out var h) && h.TryGetInt32(out var hv) ? hv : 0;

        return new PexelsPhoto(imageUrl!, attribution, sourceUrl, width, height);
    }

    private sealed record PexelsPhoto(
        string DownloadUrl, string Attribution, string? SourceUrl, int Width, int Height);
}
