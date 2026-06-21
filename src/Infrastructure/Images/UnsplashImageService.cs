using System.Collections.Concurrent;
using System.Text.Json;
using Application.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

/// <summary>
/// <see cref="IImageService"/> backed by the Unsplash API. Fetches one licensed photo per keyword
/// and returns its URL with photographer attribution (docs/development-guide.md rule 12 - licensed source,
/// attribution preserved). <see cref="FindImageAsync"/> returns a hot-link URL (used by the Books
/// covers); <see cref="DownloadImageAsync"/> additionally downloads the image bytes so the caller
/// can persist them in the database and re-serve from there. URL lookups are cached per query
/// in-process so the same keyword is fetched at most once (rule 10). Any failure (network, rate
/// limit, no match) returns null so callers fall back to a generated placeholder - never a blocking
/// error.
/// </summary>
public sealed class UnsplashImageService : IImageService
{
    private const string SearchEndpoint = "https://api.unsplash.com/search/photos";

    private readonly HttpClient _http;
    private readonly ImageOptions _options;
    private readonly ILogger<UnsplashImageService> _logger;
    private readonly ConcurrentDictionary<string, ImageResult?> _cache = new(StringComparer.OrdinalIgnoreCase);

    public UnsplashImageService(HttpClient http, ImageOptions options, ILogger<UnsplashImageService> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(query, out var cached))
            return cached;

        // Book covers read portrait; topic thumbnails (DownloadImageAsync) read landscape.
        var photo = await SearchPhotoAsync(query, "portrait", cancellationToken);
        var result = photo is null ? null : new ImageResult(photo.DownloadUrl, photo.Attribution);
        _cache[query] = result;
        return result;
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

    // Downloads a single resolved photo's bytes. Returns null on any failure so a gallery still
    // fills with whatever other photos succeed (rule 12 - never a blocking error).
    private async Task<DownloadedImage?> DownloadOneAsync(
        UnsplashPhoto photo, string query, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(photo.DownloadUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Unsplash image download for '{Query}' returned {Status}.", query, response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return new DownloadedImage(
                bytes, contentType, "Unsplash", photo.Attribution, photo.SourceUrl, photo.Width, photo.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unsplash image download for '{Query}' failed.", query);
            return null;
        }
    }

    // Searches Unsplash for the first portrait photo matching the query (used by FindImageAsync for
    // book covers). Returns null on any failure so every caller degrades gracefully.
    private async Task<UnsplashPhoto?> SearchPhotoAsync(
        string query, string orientation, CancellationToken cancellationToken)
    {
        var photos = await SearchPhotosAsync(query, orientation, 1, cancellationToken);
        return photos.Count > 0 ? photos[0] : null;
    }

    // Searches Unsplash for up to <paramref name="count"/> photos matching the query. Returns an
    // empty list on any failure (no key, network, rate limit, no match) so callers degrade gracefully.
    private async Task<IReadOnlyList<UnsplashPhoto>> SearchPhotosAsync(
        string query, string orientation, int count, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(_options.UnsplashAccessKey))
            return Array.Empty<UnsplashPhoto>();

        try
        {
            var perPage = Math.Clamp(count * 3, 1, 30);
            var url = $"{SearchEndpoint}?query={Uri.EscapeDataString(query)}&per_page={perPage}" +
                      $"&orientation={orientation}&content_filter=high";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Client-ID {_options.UnsplashAccessKey}");
            request.Headers.Add("Accept-Version", "v1");

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Unsplash lookup for '{Query}' returned {Status}.", query, response.StatusCode);
                return Array.Empty<UnsplashPhoto>();
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array)
                return Array.Empty<UnsplashPhoto>();

            var photos = new List<UnsplashPhoto>();
            foreach (var item in results.EnumerateArray())
            {
                var photo = ParsePhoto(item);
                if (photo is not null)
                    photos.Add(photo);
                if (photos.Count >= count)
                    break;
            }

            return photos;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unsplash lookup for '{Query}' failed.", query);
            return Array.Empty<UnsplashPhoto>();
        }
    }

    private static UnsplashPhoto? ParsePhoto(JsonElement item)
    {
        var imageUrl = item.TryGetProperty("urls", out var urls) && urls.TryGetProperty("regular", out var regular)
            ? regular.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var description = item.TryGetProperty("description", out var descriptionEl)
            ? descriptionEl.GetString()
            : null;
        var altDescription = item.TryGetProperty("alt_description", out var altDescriptionEl)
            ? altDescriptionEl.GetString()
            : null;
        var author = item.TryGetProperty("user", out var user) && user.TryGetProperty("name", out var name)
            ? name.GetString()
            : null;
        if (!ImageSafetyFilter.IsSafe(description, altDescription, author))
            return null;

        var attribution = string.IsNullOrWhiteSpace(author)
            ? "Photo via Unsplash"
            : $"Photo by {author} on Unsplash";

        var sourceUrl = item.TryGetProperty("links", out var links) && links.TryGetProperty("html", out var html)
            ? html.GetString()
            : null;
        var width = item.TryGetProperty("width", out var w) && w.TryGetInt32(out var wv) ? wv : 0;
        var height = item.TryGetProperty("height", out var h) && h.TryGetInt32(out var hv) ? hv : 0;

        return new UnsplashPhoto(imageUrl!, attribution, sourceUrl, width, height);
    }

    private sealed record UnsplashPhoto(
        string DownloadUrl, string Attribution, string? SourceUrl, int Width, int Height);
}
