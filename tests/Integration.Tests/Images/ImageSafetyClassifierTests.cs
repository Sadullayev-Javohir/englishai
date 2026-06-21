using System.Net;
using System.Text;
using Application.Common;
using FluentAssertions;
using Infrastructure.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Images;

public class ImageSafetyClassifierTests
{
    [Fact]
    public async Task Http_classifier_maps_safe_response()
    {
        var classifier = new HttpImageSafetyClassifier(new HttpClient(new JsonHandler(
            """{"safe":true,"modelVersion":"nudenet-test","reasons":[]}"""))
        {
            BaseAddress = new Uri("http://moderation"),
        });

        var decision = await classifier.ClassifyAsync([1, 2, 3], "image/jpeg", default);

        decision.IsSafe.Should().BeTrue();
        decision.ModelVersion.Should().Be("nudenet-test");
    }

    [Fact]
    public async Task Safety_checked_service_skips_unsafe_candidate()
    {
        var inner = new FakeImageService(
            Image("unsafe"),
            Image("safe"));
        var classifier = new SequentialClassifier(
            new ImageSafetyDecision(false, "test", ["FEMALE_BREAST_EXPOSED:0.900"]),
            new ImageSafetyDecision(true, "test", []));
        var service = new SafetyCheckedImageService(
            inner,
            classifier,
            new ImageModerationOptions { Enabled = true, RequireSafetyApproval = true },
            NullLogger<SafetyCheckedImageService>.Instance);

        var result = await service.DownloadImageAsync("word", default);

        result.Should().NotBeNull();
        result!.SourceUrl.Should().Be("safe");
        result.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
    }

    [Fact]
    public async Task Safety_checked_service_fails_closed_when_classifier_errors()
    {
        var service = new SafetyCheckedImageService(
            new FakeImageService(Image("candidate")),
            new ThrowingClassifier(),
            new ImageModerationOptions { Enabled = true, RequireSafetyApproval = true },
            NullLogger<SafetyCheckedImageService>.Instance);

        var result = await service.DownloadImageAsync("word", default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Safety_checked_service_caps_replacement_overfetch_at_provider_limit()
    {
        var inner = new RecordingImageService();
        var service = new SafetyCheckedImageService(
            inner,
            new SequentialClassifier(),
            new ImageModerationOptions
            {
                Enabled = true,
                RequireSafetyApproval = true,
                CandidateMultiplier = 4,
            },
            NullLogger<SafetyCheckedImageService>.Instance);

        var result = await service.DownloadImagesAsync("word", 8, default);

        result.Should().BeEmpty();
        inner.RequestedCount.Should().Be(30);
    }

    private static DownloadedImage Image(string sourceUrl) =>
        new([1, 2, 3], "image/jpeg", "test", null, sourceUrl, 10, 10);

    private sealed class JsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }

    private sealed class FakeImageService(params DownloadedImage[] images) : IImageService
    {
        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult(images.FirstOrDefault());

        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query,
            int count,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DownloadedImage>>(images.Take(count).ToArray());
    }

    private sealed class SequentialClassifier(params ImageSafetyDecision[] decisions) : IImageSafetyClassifier
    {
        private int _index;

        public Task<ImageSafetyDecision> ClassifyAsync(
            byte[] data,
            string contentType,
            CancellationToken cancellationToken) =>
            Task.FromResult(decisions[_index++]);
    }

    private sealed class RecordingImageService : IImageService
    {
        public int RequestedCount { get; private set; }

        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<DownloadedImage?>(null);

        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query,
            int count,
            CancellationToken cancellationToken)
        {
            RequestedCount = count;
            return Task.FromResult<IReadOnlyList<DownloadedImage>>([]);
        }
    }

    private sealed class ThrowingClassifier : IImageSafetyClassifier
    {
        public Task<ImageSafetyDecision> ClassifyAsync(
            byte[] data,
            string contentType,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("unavailable");
    }
}
