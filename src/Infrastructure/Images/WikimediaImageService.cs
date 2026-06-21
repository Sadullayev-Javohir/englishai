using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

/// <summary>
/// Keyless <see cref="IImageService"/> backed by the Wikimedia Commons API. Commons hosts only
/// freely licensed media (Creative Commons / public domain), so it satisfies docs/development-guide.md rule 12
/// (licensed/open source, attribution preserved) without any API key - it is the always-available
/// fallback in the provider chain so topic galleries fill even when no Unsplash/Pexels key is set.
/// Search is restricted to bitmap photos (<c>filetype:bitmap</c>); each result's bytes are fetched
/// from a width-capped thumbnail so stored images stay a sensible size. Every metadata field
/// (artist, license, description page) is preserved as the attribution. Any failure (network, no
/// match, non-image) returns null/empty so callers degrade gracefully - never a blocking error.
/// </summary>
public sealed class WikimediaImageService : IImageService
{
    private const string ApiEndpoint = "https://commons.wikimedia.org/w/api.php";
    private const int ThumbnailWidth = 1024;
    private const int MaxDownloadAttempts = 3;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
    private static readonly SemaphoreSlim ThumbnailDownloadGate = new(1, 1);

    private static readonly Regex HtmlTag = new("<[^>]+>", RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly ILogger<WikimediaImageService> _logger;

    public WikimediaImageService(HttpClient http, ILogger<WikimediaImageService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken)
    {
        var photos = await SearchPhotosAsync(query, 1, cancellationToken);
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

        var photos = await SearchPhotosAsync(query, Math.Clamp(count * 3, count, 30), cancellationToken);
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

    // Downloads a single resolved photo's bytes from its thumbnail URL. Returns null on any failure
    // so a gallery still fills with whatever other photos succeed (rule 12 - never a blocking error).
    private async Task<DownloadedImage?> DownloadOneAsync(
        WikimediaPhoto photo, string query, CancellationToken cancellationToken)
    {
        await ThumbnailDownloadGate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 1; attempt <= MaxDownloadAttempts; attempt++)
            {
                using var response = await _http.GetAsync(photo.DownloadUrl, cancellationToken);
                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxDownloadAttempts)
                {
                    var retryDelay = response.Headers.RetryAfter?.Delta ?? DefaultRetryDelay;
                    retryDelay = retryDelay < TimeSpan.Zero
                        ? TimeSpan.Zero
                        : retryDelay > MaxRetryDelay ? MaxRetryDelay : retryDelay;
                    _logger.LogWarning(
                        "Wikimedia image download for '{Query}' was rate limited; retrying attempt {Attempt}.",
                        query,
                        attempt + 1);
                    await Task.Delay(retryDelay, cancellationToken);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Wikimedia image download for '{Query}' returned {Status}.", query, response.StatusCode);
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                if (bytes.Length == 0)
                    return null;

                var contentType = response.Content.Headers.ContentType?.MediaType ?? photo.Mime ?? "image/jpeg";
                return new DownloadedImage(
                    bytes, contentType, "Wikimedia Commons", photo.Attribution, photo.SourceUrl, photo.Width, photo.Height);
            }

            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wikimedia image download for '{Query}' failed.", query);
            return null;
        }
        finally
        {
            ThumbnailDownloadGate.Release();
        }
    }

    // Searches Commons for up to <paramref name="count"/> bitmap photos matching the query. Returns an
    // empty list on any failure (network, no match) so callers degrade gracefully. Requests a couple
    // extra results so non-image/oversized matches can be skipped while still reaching the count.
    private async Task<IReadOnlyList<WikimediaPhoto>> SearchPhotosAsync(
        string query, int count, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<WikimediaPhoto>();

        try
        {
            // Over-fetch generously: results are dropped for non-image MIME, oversize, and unsafe
            // content, so a small buffer can starve a gallery - ask for plenty and trim to count.
            var limit = Math.Clamp(count * 2 + 6, 1, 50);
            var search = Uri.EscapeDataString($"{query} filetype:bitmap");
            var url = $"{ApiEndpoint}?action=query&format=json&generator=search" +
                      $"&gsrsearch={search}&gsrnamespace=6&gsrlimit={limit}" +
                      $"&prop=imageinfo&iiprop=url%7Csize%7Cmime%7Cextmetadata&iiurlwidth={ThumbnailWidth}";

            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikimedia lookup for '{Query}' returned {Status}.", query, response.StatusCode);
                return Array.Empty<WikimediaPhoto>();
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("query", out var queryEl) ||
                !queryEl.TryGetProperty("pages", out var pages) ||
                pages.ValueKind != JsonValueKind.Object)
                return Array.Empty<WikimediaPhoto>();

            // Search results carry an "index"; the pages object isn't ordered, so sort by it.
            var photos = new List<(int Index, WikimediaPhoto Photo)>();
            foreach (var page in pages.EnumerateObject())
            {
                var photo = ParsePhoto(page.Value);
                if (photo is not null)
                {
                    var index = page.Value.TryGetProperty("index", out var idx) && idx.TryGetInt32(out var iv) ? iv : int.MaxValue;
                    photos.Add((index, photo));
                }
            }

            return photos.OrderBy(p => p.Index).Select(p => p.Photo).Take(count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wikimedia lookup for '{Query}' failed.", query);
            return Array.Empty<WikimediaPhoto>();
        }
    }

    private static WikimediaPhoto? ParsePhoto(JsonElement page)
    {
        if (!page.TryGetProperty("imageinfo", out var infos) ||
            infos.ValueKind != JsonValueKind.Array ||
            infos.GetArrayLength() == 0)
            return null;

        // Skip files whose Commons title flags explicit/adult content before doing any further work
        // (the title is the most reliable signal of what the file depicts).
        var title = page.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
        if (!ImageSafetyFilter.IsSafe(title))
            return null;

        var info = infos[0];

        var mime = info.TryGetProperty("mime", out var mimeEl) ? mimeEl.GetString() : null;
        if (mime is not ("image/jpeg" or "image/png"))
            return null;

        // Prefer the width-capped thumbnail so stored bytes stay reasonable; fall back to the original.
        var imageUrl = info.TryGetProperty("thumburl", out var thumb) ? thumb.GetString() : null;
        if (string.IsNullOrWhiteSpace(imageUrl))
            imageUrl = info.TryGetProperty("url", out var orig) ? orig.GetString() : null;
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var width = info.TryGetProperty("thumbwidth", out var tw) && tw.TryGetInt32(out var twv) ? twv
            : info.TryGetProperty("width", out var w) && w.TryGetInt32(out var wv) ? wv : 0;
        var height = info.TryGetProperty("thumbheight", out var th) && th.TryGetInt32(out var thv) ? thv
            : info.TryGetProperty("height", out var h) && h.TryGetInt32(out var hv) ? hv : 0;

        // SourceUrl is varchar(500); a pathologically long file URL is dropped rather than break the save.
        var sourceUrl = info.TryGetProperty("descriptionurl", out var desc) ? desc.GetString() : null;
        if (sourceUrl is { Length: > 500 })
            sourceUrl = null;

        var attribution = BuildAttribution(info);
        if (!ImageSafetyFilter.IsSafe(attribution))
            return null;

        return new WikimediaPhoto(imageUrl!, attribution, sourceUrl, mime, width, height);
    }

    // The Attribution column is varchar(300); a pathological multi-author credit can exceed that, so
    // the display string is capped to fit (it's human-readable credit, not data - truncation is safe).
    private const int MaxAttributionLength = 300;

    // Builds a human-readable attribution from the Commons extmetadata (artist + license). Artist
    // values are HTML fragments (links/markup), so tags are stripped and entities decoded.
    private static string BuildAttribution(JsonElement info)
    {
        string? artist = null;
        string? license = null;
        if (info.TryGetProperty("extmetadata", out var meta) && meta.ValueKind == JsonValueKind.Object)
        {
            artist = CleanText(ReadMeta(meta, "Artist"));
            license = CleanText(ReadMeta(meta, "LicenseShortName"));
        }

        var credit = string.IsNullOrWhiteSpace(artist) ? null : artist;
        if (!string.IsNullOrWhiteSpace(license))
            credit = credit is null ? license : $"{credit} ({license})";

        var attribution = string.IsNullOrWhiteSpace(credit)
            ? "Via Wikimedia Commons"
            : $"{credit} via Wikimedia Commons";

        return attribution.Length <= MaxAttributionLength
            ? attribution
            : attribution[..(MaxAttributionLength - 1)].TrimEnd() + "…";
    }

    private static string? ReadMeta(JsonElement meta, string key) =>
        meta.TryGetProperty(key, out var field) && field.TryGetProperty("value", out var val)
            ? val.GetString()
            : null;

    private static string? CleanText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return null;
        var stripped = WebUtility.HtmlDecode(HtmlTag.Replace(html, " ")).Trim();
        return Regex.Replace(stripped, "\\s+", " ");
    }

    private sealed record WikimediaPhoto(
        string DownloadUrl, string Attribution, string? SourceUrl, string? Mime, int Width, int Height);
}
