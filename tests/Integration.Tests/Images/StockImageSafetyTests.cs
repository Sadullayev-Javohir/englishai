using System.Net;
using System.Text;
using FluentAssertions;
using Infrastructure.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Images;

public class StockImageSafetyTests
{
    [Fact]
    public async Task Unsplash_skips_unsafe_metadata_and_uses_the_next_safe_photo()
    {
        const string json = """
            {"results":[
              {"description":"woman in bikini at beach","alt_description":"summer portrait","urls":{"regular":"https://images.example/unsafe.jpg"},"user":{"name":"First"},"links":{"html":"https://unsplash.com/unsafe"},"width":1200,"height":800},
              {"description":"quiet library shelves","alt_description":"books in a library","urls":{"regular":"https://images.example/safe.jpg"},"user":{"name":"Second"},"links":{"html":"https://unsplash.com/safe"},"width":1200,"height":800}
            ]}
            """;
        var options = new ImageOptions { UnsplashAccessKey = "test-key" };
        var service = new UnsplashImageService(
            new HttpClient(new SearchThenImageHandler(json)),
            options,
            NullLogger<UnsplashImageService>.Instance);

        var images = await service.DownloadImagesAsync("library", 1, CancellationToken.None);

        images.Should().ContainSingle();
        images[0].SourceUrl.Should().Be("https://unsplash.com/safe");
    }

    [Fact]
    public async Task Pexels_skips_unsafe_metadata_and_uses_the_next_safe_photo()
    {
        const string json = """
            {"photos":[
              {"alt":"shirtless person at a gym","src":{"large":"https://images.example/unsafe.jpg"},"photographer":"First","url":"https://pexels.com/unsafe","width":1200,"height":800},
              {"alt":"modern gym equipment","src":{"large":"https://images.example/safe.jpg"},"photographer":"Second","url":"https://pexels.com/safe","width":1200,"height":800}
            ]}
            """;
        var options = new ImageOptions { PexelsApiKey = "test-key" };
        var service = new PexelsImageService(
            new HttpClient(new SearchThenImageHandler(json)),
            options,
            NullLogger<PexelsImageService>.Instance);

        var images = await service.DownloadImagesAsync("gym", 1, CancellationToken.None);

        images.Should().ContainSingle();
        images[0].SourceUrl.Should().Be("https://pexels.com/safe");
    }

    private sealed class SearchThenImageHandler(string searchJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.Host.Contains("api.", StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(searchJson, Encoding.UTF8, "application/json"),
                });
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3]),
            };
            response.Content.Headers.ContentType = new("image/jpeg");
            return Task.FromResult(response);
        }
    }
}
