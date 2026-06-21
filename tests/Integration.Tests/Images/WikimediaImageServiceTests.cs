using System.Net;
using System.Text;
using FluentAssertions;
using Infrastructure.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Images;

/// <summary>
/// Verifies the keyless Wikimedia Commons image provider parses the MediaWiki API response,
/// keeps results in search order, skips non-photo (e.g. SVG) results, prefers the width-capped
/// thumbnail, builds an attribution from the Commons extmetadata (stripping HTML), and downloads
/// the bytes - all against a captured-shape fixture with no real network call.
/// </summary>
public class WikimediaImageServiceTests
{
    private const string SearchJson = """
        {"query":{"pages":{
          "20":{"pageid":20,"title":"File:Cat photo.jpg","index":2,"imageinfo":[{
            "thumburl":"https://upload.wikimedia.org/thumb/cat-1024.jpg","thumbwidth":1024,"thumbheight":683,
            "url":"https://upload.wikimedia.org/cat-original.jpg","descriptionurl":"https://commons.wikimedia.org/wiki/File:Cat_photo.jpg",
            "mime":"image/jpeg","extmetadata":{
              "Artist":{"value":"<a href=\"/wiki/User:Jane\">Jane Doe</a>"},
              "LicenseShortName":{"value":"CC BY-SA 4.0"}}}]},
          "10":{"pageid":10,"title":"File:Cat best.jpg","index":1,"imageinfo":[{
            "thumburl":"https://upload.wikimedia.org/thumb/cat-best-1024.jpg","thumbwidth":1024,"thumbheight":768,
            "descriptionurl":"https://commons.wikimedia.org/wiki/File:Cat_best.jpg",
            "mime":"image/jpeg","extmetadata":{"LicenseShortName":{"value":"Public domain"}}}]},
          "30":{"pageid":30,"title":"File:Cat diagram.svg","index":3,"imageinfo":[{
            "thumburl":"https://upload.wikimedia.org/thumb/cat.svg","mime":"image/svg+xml"}]}
        }}}
        """;

    // Returns the search JSON for api.php calls and 3 image bytes for thumbnail downloads, so the
    // service exercises both the search-parse path and the byte-download path.
    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            HttpResponseMessage response;
            if (url.Contains("api.php"))
            {
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SearchJson, Encoding.UTF8, "application/json"),
                };
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 }),
                };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            }

            return Task.FromResult(response);
        }
    }

    private static WikimediaImageService NewService() =>
        new(new HttpClient(new StubHandler()), NullLogger<WikimediaImageService>.Instance);

    [Fact]
    public async Task Download_orders_by_search_index_skips_non_photos_and_keeps_provenance()
    {
        var images = await NewService().DownloadImagesAsync("cat", 5, CancellationToken.None);

        // The SVG result is skipped; the two JPEG photos come back in search-index order (best first).
        images.Should().HaveCount(2);
        images[0].SourceUrl.Should().Be("https://commons.wikimedia.org/wiki/File:Cat_best.jpg");
        images[0].Attribution.Should().Be("Public domain via Wikimedia Commons");
        images[0].Data.Should().Equal(1, 2, 3);
        images[0].ContentType.Should().Be("image/jpeg");
        images[0].Source.Should().Be("Wikimedia Commons");
        images[0].Width.Should().Be(1024);
        images[0].Height.Should().Be(768);
    }

    [Fact]
    public async Task Attribution_strips_html_and_combines_artist_with_license()
    {
        var images = await NewService().DownloadImagesAsync("cat", 5, CancellationToken.None);

        var withArtist = images.Single(i => i.SourceUrl!.Contains("Cat_photo"));
        withArtist.Attribution.Should().Be("Jane Doe (CC BY-SA 4.0) via Wikimedia Commons");
    }

    [Fact]
    public async Task Attribution_is_capped_to_the_column_length()
    {
        var longArtist = new string('A', 600);
        var json = "{\"query\":{\"pages\":{\"1\":{\"index\":1,\"imageinfo\":[{" +
                   "\"thumburl\":\"https://upload.wikimedia.org/thumb/x-1024.jpg\",\"thumbwidth\":1024,\"thumbheight\":768," +
                   "\"descriptionurl\":\"https://commons.wikimedia.org/wiki/File:X.jpg\",\"mime\":\"image/jpeg\"," +
                   "\"extmetadata\":{\"Artist\":{\"value\":\"" + longArtist + "\"},\"LicenseShortName\":{\"value\":\"CC BY 4.0\"}}}]}}}}";
        var service = new WikimediaImageService(new HttpClient(new FixedJsonHandler(json)), NullLogger<WikimediaImageService>.Instance);

        var images = await service.DownloadImagesAsync("x", 1, CancellationToken.None);

        images.Should().ContainSingle();
        images[0].Attribution!.Length.Should().BeLessThanOrEqualTo(300, "the Attribution column is varchar(300)");
        images[0].Attribution.Should().EndWith("…");
    }

    private sealed class FixedJsonHandler : HttpMessageHandler
    {
        private readonly string _json;
        public FixedJsonHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            HttpResponseMessage response;
            if (url.Contains("api.php"))
                response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_json, Encoding.UTF8, "application/json") };
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[] { 9 }) };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            }
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task Skips_files_whose_title_or_attribution_flags_explicit_content()
    {
        // Slot 1 has an adult title, slot 2 a clean title but an adult artist credit, slot 3 is clean.
        // Only the clean file should survive - the app teaches children, so nudity/explicit results
        // must never reach a topic gallery (docs/development-guide.md rule 12: safe, vetted content only).
        var json = "{\"query\":{\"pages\":{" +
            "\"1\":{\"index\":1,\"title\":\"File:Kenyon Cox nude study.jpg\",\"imageinfo\":[{" +
            "\"thumburl\":\"https://upload.wikimedia.org/thumb/nude-1024.jpg\",\"thumbwidth\":1024,\"thumbheight\":768," +
            "\"descriptionurl\":\"https://commons.wikimedia.org/wiki/File:Nude.jpg\",\"mime\":\"image/jpeg\"}]}," +
            "\"2\":{\"index\":2,\"title\":\"File:Figure study.jpg\",\"imageinfo\":[{" +
            "\"thumburl\":\"https://upload.wikimedia.org/thumb/erotic-1024.jpg\",\"thumbwidth\":1024,\"thumbheight\":768," +
            "\"descriptionurl\":\"https://commons.wikimedia.org/wiki/File:Figure.jpg\",\"mime\":\"image/jpeg\"," +
            "\"extmetadata\":{\"Artist\":{\"value\":\"An erotic sketch artist\"}}}]}," +
            "\"3\":{\"index\":3,\"title\":\"File:Child crayon drawing.jpg\",\"imageinfo\":[{" +
            "\"thumburl\":\"https://upload.wikimedia.org/thumb/clean-1024.jpg\",\"thumbwidth\":1024,\"thumbheight\":768," +
            "\"descriptionurl\":\"https://commons.wikimedia.org/wiki/File:Clean.jpg\",\"mime\":\"image/jpeg\"}]}" +
            "}}}";
        var service = new WikimediaImageService(new HttpClient(new FixedJsonHandler(json)), NullLogger<WikimediaImageService>.Instance);

        var images = await service.DownloadImagesAsync("drawing", 5, CancellationToken.None);

        images.Should().ContainSingle();
        images[0].SourceUrl.Should().Be("https://commons.wikimedia.org/wiki/File:Clean.jpg");
    }

    [Fact]
    public async Task Does_not_flag_safe_words_that_contain_a_blocked_substring()
    {
        // Whole-word matching: "Essex" must not trip the "sex" rule.
        var json = "{\"query\":{\"pages\":{\"1\":{\"index\":1,\"title\":\"File:Essex village fair.jpg\",\"imageinfo\":[{" +
            "\"thumburl\":\"https://upload.wikimedia.org/thumb/essex-1024.jpg\",\"thumbwidth\":1024,\"thumbheight\":768," +
            "\"descriptionurl\":\"https://commons.wikimedia.org/wiki/File:Essex.jpg\",\"mime\":\"image/jpeg\"}]}}}}";
        var service = new WikimediaImageService(new HttpClient(new FixedJsonHandler(json)), NullLogger<WikimediaImageService>.Instance);

        var images = await service.DownloadImagesAsync("fair", 5, CancellationToken.None);

        images.Should().ContainSingle();
    }

    [Fact]
    public async Task Find_image_returns_the_top_result_url()
    {
        var result = await NewService().FindImageAsync("cat", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Url.Should().Be("https://upload.wikimedia.org/thumb/cat-best-1024.jpg");
    }

    [Fact]
    public async Task Download_retries_thumbnail_after_too_many_requests()
    {
        var handler = new RateLimitedThumbnailHandler();
        var service = new WikimediaImageService(
            new HttpClient(handler), NullLogger<WikimediaImageService>.Instance);

        var images = await service.DownloadImagesAsync("cat", 1, CancellationToken.None);

        images.Should().ContainSingle();
        images[0].Data.Should().Equal(7, 8, 9);
        handler.ThumbnailRequests.Should().Be(2);
    }

    [Fact]
    public async Task Concurrent_downloads_are_serialized_for_the_thumbnail_host()
    {
        var handler = new ConcurrentThumbnailHandler();
        var service = new WikimediaImageService(
            new HttpClient(handler), NullLogger<WikimediaImageService>.Instance);

        await Task.WhenAll(
            service.DownloadImagesAsync("cat", 1, CancellationToken.None),
            service.DownloadImagesAsync("cat", 1, CancellationToken.None));

        handler.MaxConcurrentThumbnailRequests.Should().Be(1);
    }

    private sealed class ConcurrentThumbnailHandler : HttpMessageHandler
    {
        private int _active;
        public int MaxConcurrentThumbnailRequests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("api.php", StringComparison.Ordinal))
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        RateLimitedThumbnailHandler.SingleImageJson,
                        Encoding.UTF8,
                        "application/json"),
                };

            var active = Interlocked.Increment(ref _active);
            MaxConcurrentThumbnailRequests = Math.Max(MaxConcurrentThumbnailRequests, active);
            await Task.Delay(30, cancellationToken);
            Interlocked.Decrement(ref _active);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3]),
            };
            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            return response;
        }
    }

    private sealed class RateLimitedThumbnailHandler : HttpMessageHandler
    {
        public const string SingleImageJson = """
            {"query":{"pages":{"10":{"pageid":10,"title":"File:Cat best.jpg","index":1,"imageinfo":[{
              "thumburl":"https://upload.wikimedia.org/thumb/cat-best-1024.jpg","thumbwidth":1024,"thumbheight":768,
              "descriptionurl":"https://commons.wikimedia.org/wiki/File:Cat_best.jpg",
              "mime":"image/jpeg","extmetadata":{"LicenseShortName":{"value":"Public domain"}}}]}}}}
            """;

        public int ThumbnailRequests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("api.php", StringComparison.Ordinal))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(SingleImageJson, Encoding.UTF8, "application/json"),
                });

            ThumbnailRequests++;
            if (ThumbnailRequests == 1)
            {
                var limited = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                limited.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    TimeSpan.Zero);
                return Task.FromResult(limited);
            }

            var success = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([7, 8, 9]),
            };
            success.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            return Task.FromResult(success);
        }
    }
}
