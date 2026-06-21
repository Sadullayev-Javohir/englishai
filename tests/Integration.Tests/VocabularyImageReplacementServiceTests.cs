using Application.Common;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Images;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Integration.Tests;

public sealed class VocabularyImageReplacementServiceTests
{
    [Fact]
    public async Task Replace_stores_a_different_safe_webp_and_updates_provenance()
    {
        var topic = VocabularyTopic.Curate(
            "a1-family", "Family", "Oila", "people", "present-simple",
            CefrLevel.A1, DateTimeOffset.UtcNow);
        topic.FillContent("My family.", [TopicWord.Create("mother", "ona", "My mother smiles.", PartOfSpeech.Noun)]);
        var imageId = WordImageQuery.ImageId(topic.Id, "mother");
        var store = new InMemoryTopicImageStore();
        var oldBytes = Png(new Rgb24(220, 20, 20));
        await store.SaveAsync(new TopicImageContent(
            imageId, 0, oldBytes, "image/png", "old", null, "https://old", 8, 8,
            DateTimeOffset.UtcNow, SafetyStatus: ImageSafetyStatus.Safe), default);
        topic.Words[0].SetImage($"local:{imageId}", "old", null);
        var repository = new FakeTopicRepository(topic);
        var replacement = new DownloadedImage(
            Png(new Rgb24(20, 180, 80)), "image/png", "Unsplash", "Photo by Test",
            "https://new", 8, 8, ImageSafetyStatus.Safe, "moderator-v1", DateTimeOffset.UtcNow);
        var service = new VocabularyImageReplacementService(
            repository, new FakeImageService(replacement), store, TimeProvider.System,
            NullLogger<VocabularyImageReplacementService>.Instance);

        var result = await service.ReplaceAsync(topic.Id, imageId, default);

        result.Word.Should().Be("mother");
        result.Translation.Should().Be("ona");
        result.ImageSource.Should().Be("Unsplash");
        result.Version.Should().BeGreaterThan(0);
        topic.Words[0].ImageSource.Should().Be("Unsplash");
        var stored = await store.GetByTopicIdAsync(imageId, default);
        stored.Should().NotBeNull();
        stored!.ContentType.Should().Be("image/webp");
        stored.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
        stored.SourceUrl.Should().Be("https://new");
        stored.Data!.AsSpan(0, 4).ToArray().Should().Equal("RIFF"u8.ToArray());
        repository.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Replace_generates_exact_word_webp_when_no_safe_provider_candidate_exists()
    {
        var topic = VocabularyTopic.Curate(
            "a1-family", "Family", "Oila", "people", "present-simple",
            CefrLevel.A1, DateTimeOffset.UtcNow);
        topic.FillContent("My family.", [TopicWord.Create("mother", "ona")]);
        var imageId = WordImageQuery.ImageId(topic.Id, "mother");
        var store = new InMemoryTopicImageStore();
        var unsafeCandidate = new DownloadedImage(
            Png(new Rgb24(20, 20, 20)), "image/png", "test", null, "https://unsafe",
            8, 8, ImageSafetyStatus.Unsafe);
        var service = new VocabularyImageReplacementService(
            new FakeTopicRepository(topic), new FakeImageService(unsafeCandidate), store,
            TimeProvider.System, NullLogger<VocabularyImageReplacementService>.Instance);

        var result = await service.ReplaceAsync(topic.Id, imageId, default);

        result.ImageSource.Should().Be("EnglishAI Generated");
        result.ImageAttribution.Should().Contain("generated vocabulary illustration");
        var stored = await store.GetByTopicIdAsync(imageId, default);
        stored.Should().NotBeNull();
        stored!.ContentType.Should().Be("image/webp");
        stored.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
        stored.SourceUrl.Should().StartWith("generated:vocabulary:");
        stored.Width.Should().Be(640);
        stored.Height.Should().Be(480);
        stored.Data!.AsSpan(0, 4).ToArray().Should().Equal("RIFF"u8.ToArray());
    }

    [Fact]
    public async Task Replace_uses_generated_webp_when_provider_exceeds_the_search_deadline()
    {
        var topic = VocabularyTopic.Curate(
            "a1-family", "Family", "Oila", "people", "present-simple",
            CefrLevel.A1, DateTimeOffset.UtcNow);
        topic.FillContent("My family.", [TopicWord.Create("mother", "ona")]);
        var imageId = WordImageQuery.ImageId(topic.Id, "mother");
        var store = new InMemoryTopicImageStore();
        var imageService = new NeverCompletingImageService();
        var service = new VocabularyImageReplacementService(
            new FakeTopicRepository(topic), imageService, store,
            TimeProvider.System, NullLogger<VocabularyImageReplacementService>.Instance);

        var result = await service.ReplaceAsync(topic.Id, imageId, default);

        result.ImageSource.Should().Be("EnglishAI Generated");
        imageService.ProviderCallWasCancelled.Should().BeTrue(
            "the provider deadline must cancel a stalled provider before generated fallback is used");
        var stored = await store.GetByTopicIdAsync(imageId, default);
        stored.Should().NotBeNull();
        stored!.ContentType.Should().Be("image/webp");
        stored.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
    }

    private static byte[] Png(Rgb24 color)
    {
        using var image = new Image<Rgb24>(8, 8, color);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    private sealed class FakeImageService(DownloadedImage image) : IImageService
    {
        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) => Task.FromResult<ImageResult?>(null);
        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken) => Task.FromResult<DownloadedImage?>(image);
        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(string query, int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DownloadedImage>>([image]);
    }

    private sealed class NeverCompletingImageService : IImageService
    {
        public bool ProviderCallWasCancelled { get; private set; }

        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<DownloadedImage?>(null);

        public async Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query,
            int count,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                ProviderCallWasCancelled = true;
                throw;
            }
            return [];
        }
    }

    private sealed class FakeTopicRepository(VocabularyTopic topic) : IVocabularyTopicRepository
    {
        public int SaveCount { get; private set; }
        public Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<VocabularyTopic?>(id == topic.Id ? topic : null);
        public Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VocabularyTopic>>(ids.Contains(topic.Id) ? [topic] : []);
        public Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VocabularyTopic>>(level == topic.Level ? [topic] : []);
        public Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<VocabularyTopic>>([topic]);
        public Task SaveAsync(VocabularyTopic value, CancellationToken cancellationToken) { SaveCount++; return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
