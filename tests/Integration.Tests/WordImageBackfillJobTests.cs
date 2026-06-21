using Application.Common;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Images;
using Infrastructure.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Integration.Tests;

public class WordImageBackfillJobTests
{
    private static readonly byte[] TestPng = CreateTestPng();
    private static readonly byte[] TestWebp = WebpConverter.ToWebp(TestPng, "image/png").Data;

    private static byte[] CreateTestPng()
    {
        using var image = new Image<Rgba32>(1, 1, Color.CornflowerBlue);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    [Fact]
    public async Task Stores_one_semantic_local_image_per_word_and_is_idempotent()
    {
        var topic = VocabularyTopic.Curate(
            "a1-family",
            "My Family",
            "Mening oilam",
            "family",
            "present-simple",
            CefrLevel.A1,
            DateTimeOffset.UtcNow);
        topic.FillContent("My family is kind.",
        [
            TopicWord.Create("family", "oila", "My family is kind.", PartOfSpeech.Noun),
            TopicWord.Create("mother", "ona", "My mother is kind.", PartOfSpeech.Noun),
        ]);
        var topics = new FakeTopicRepository(topic);
        var images = new FakeImageService();
        var store = new InMemoryTopicImageStore();
        var job = new WordImageBackfillJob(
            topics,
            images,
            store,
            TimeProvider.System,
            NullLogger<WordImageBackfillJob>.Instance,
            throttleDelay: TimeSpan.Zero);

        var first = await job.RunAsync();
        var second = await job.RunAsync();

        first.Stored.Should().Be(2);
        first.WordsConsidered.Should().Be(2);
        second.Stored.Should().Be(0);
        second.AlreadyHadImage.Should().Be(2);
        images.Queries.Should().HaveCount(2);
        images.Queries.Should().Contain(query => query.Contains("family", StringComparison.OrdinalIgnoreCase));
        images.Queries.Should().Contain(query => query.Contains("mother", StringComparison.OrdinalIgnoreCase));
        images.Queries.Should().OnlyHaveUniqueItems();
        topic.Words.Should().OnlyContain(word => word.ImageUrl!.StartsWith("local:", StringComparison.Ordinal));

        foreach (var word in topic.Words)
        {
            var id = WordImageQuery.ImageId(topic.Id, word.Word);
            var stored = await store.GetByTopicIdAsync(id, default);
            stored.Should().NotBeNull();
            stored!.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
        }
    }

    [Fact]
    public async Task Pending_local_image_is_replaced_instead_of_being_trusted()
    {
        var topic = VocabularyTopic.Curate(
            "a1-family-pending",
            "My Family",
            "Mening oilam",
            "family",
            "present-simple",
            CefrLevel.A1,
            DateTimeOffset.UtcNow);
        topic.FillContent("My family is kind.",
        [
            TopicWord.Create("family", "oila", "My family is kind.", PartOfSpeech.Noun),
        ]);
        var store = new InMemoryTopicImageStore();
        var job = new WordImageBackfillJob(
            new FakeTopicRepository(topic),
            new FakeImageService(ImageSafetyStatus.Safe),
            store,
            TimeProvider.System,
            NullLogger<WordImageBackfillJob>.Instance,
            throttleDelay: TimeSpan.Zero);

        var imageId = WordImageQuery.ImageId(topic.Id, "family");
        var oldImage = TestPng;
        await store.SaveAsync(
            new TopicImageContent(
                imageId, 0, oldImage, "image/png", "old", null, null, 1, 1,
                DateTimeOffset.UtcNow, SafetyStatus: ImageSafetyStatus.Pending),
            default);
        topic.Words.Single().SetImage($"local:{imageId}", "old", null);

        var result = await job.RunAsync();

        result.Stored.Should().Be(1);
        result.AlreadyHadImage.Should().Be(0);
        topic.Words.Single().ImageUrl.Should().StartWith("local:");
        var stored = await store.GetByTopicIdAsync(imageId, default);
        stored.Should().NotBeNull();
        stored!.SafetyStatus.Should().Be(ImageSafetyStatus.Safe);
        stored.Data.Should().NotEqual(oldImage);
    }

    [Fact]
    public async Task Safe_non_webp_local_image_is_replaced_to_satisfy_the_storage_contract()
    {
        var topic = VocabularyTopic.Curate(
            "a1-family-jpeg",
            "My Family",
            "Mening oilam",
            "family",
            "present-simple",
            CefrLevel.A1,
            DateTimeOffset.UtcNow);
        topic.FillContent("My family is kind.",
        [
            TopicWord.Create("family", "oila", "My family is kind.", PartOfSpeech.Noun),
        ]);
        var store = new InMemoryTopicImageStore();
        var images = new FakeImageService(ImageSafetyStatus.Safe);
        var job = new WordImageBackfillJob(
            new FakeTopicRepository(topic),
            images,
            store,
            TimeProvider.System,
            NullLogger<WordImageBackfillJob>.Instance,
            throttleDelay: TimeSpan.Zero);

        var imageId = WordImageQuery.ImageId(topic.Id, "family");
        await store.SaveAsync(
            new TopicImageContent(
                imageId, 0, [0xFF, 0xD8, 0xFF, 0xD9], "image/jpeg", "old", null, null, 1, 1,
                DateTimeOffset.UtcNow, SafetyStatus: ImageSafetyStatus.Safe),
            default);
        topic.Words.Single().SetImage($"local:{imageId}", "old", null);

        var result = await job.RunAsync();

        result.Stored.Should().Be(1);
        result.AlreadyHadImage.Should().Be(0);
        images.Queries.Should().ContainSingle();
        var stored = await store.GetByTopicIdAsync(imageId, default);
        stored.Should().NotBeNull();
        stored!.ContentType.Should().Be("image/webp");
        stored.Data.Should().NotBeNull();
        stored.Data!.AsSpan(0, 4).ToArray().Should().Equal("RIFF"u8.ToArray());
        stored.Data.AsSpan(8, 4).ToArray().Should().Equal("WEBP"u8.ToArray());
    }

    [Fact]
    public async Task Failed_pos_aware_lookup_retries_with_the_semantic_head()
    {
        var topic = VocabularyTopic.Curate(
            "a1-action-fallback", "Actions", "Harakatlar", "actions", "present-simple",
            CefrLevel.A1, DateTimeOffset.UtcNow);
        topic.FillContent("I run every morning.",
        [
            TopicWord.Create("run", "yugurmoq", "I run every morning.", PartOfSpeech.Verb),
        ]);
        var images = new SemanticFallbackImageService();
        var store = new InMemoryTopicImageStore();
        var job = new WordImageBackfillJob(
            new FakeTopicRepository(topic), images, store, TimeProvider.System,
            NullLogger<WordImageBackfillJob>.Instance, throttleDelay: TimeSpan.Zero);

        var result = await job.RunAsync();

        result.Stored.Should().Be(1);
        images.Queries.Should().Equal("person run action", "run");
        var imageId = WordImageQuery.ImageId(topic.Id, "run");
        (await store.GetByTopicIdAsync(imageId, default))!.ContentType.Should().Be("image/webp");
    }

    [Fact]
    public async Task Failed_image_in_first_batch_does_not_block_the_next_hundred_words()
    {
        var topics = Enumerable.Range(0, 6).Select(topicIndex =>
        {
            var topic = VocabularyTopic.Curate(
                $"a1-batch-{topicIndex}", $"Batch {topicIndex}", $"Paket {topicIndex}",
                "batch", "present-simple", CefrLevel.A1, DateTimeOffset.UtcNow);
            topic.FillContent("Batch words.", Enumerable.Range(0, 20)
                .Select(wordIndex => TopicWord.Create(
                    $"word-{topicIndex:D2}-{wordIndex:D2}",
                    $"soz-{topicIndex:D2}-{wordIndex:D2}",
                    "A safe example.",
                    PartOfSpeech.Noun))
                .ToArray());
            return topic;
        }).ToArray();
        var images = new FirstLookupFailsImageService();
        var scheduler = new RecordingScheduler();
        var job = new WordImageBackfillJob(
            new FakeTopicRepository(topics), images, new InMemoryTopicImageStore(), TimeProvider.System,
            NullLogger<WordImageBackfillJob>.Instance, scheduler, TimeSpan.Zero);

        var first = await job.RunAsync();
        var second = await job.RunBatchAsync(100);

        first.WordsConsidered.Should().Be(100);
        first.Stored.Should().Be(99);
        second.WordsConsidered.Should().Be(20);
        second.Stored.Should().Be(20);
        scheduler.ContentJobs.Should().ContainSingle(jobExpression =>
            jobExpression.Contains("RunBatchAsync", StringComparison.Ordinal));
    }

    [Fact]
    public async Task All_safe_first_batch_still_queues_the_next_page()
    {
        var topics = Enumerable.Range(0, 6).Select(topicIndex =>
        {
            var topic = VocabularyTopic.Curate(
                $"a1-safe-{topicIndex}", $"Safe {topicIndex}", $"Xavfsiz {topicIndex}",
                "safe", "present-simple", CefrLevel.A1, DateTimeOffset.UtcNow);
            topic.FillContent("Safe words.", Enumerable.Range(0, 20)
                .Select(wordIndex => TopicWord.Create(
                    $"safe-{topicIndex:D2}-{wordIndex:D2}",
                    $"xavfsiz-{topicIndex:D2}-{wordIndex:D2}",
                    "A safe example.",
                    PartOfSpeech.Noun))
                .ToArray());
            return topic;
        }).ToArray();
        var store = new InMemoryTopicImageStore();
        foreach (var topic in topics.Take(5))
        foreach (var word in topic.Words)
        {
            var id = WordImageQuery.ImageId(topic.Id, word.Word);
            await store.SaveAsync(new TopicImageContent(
                id, 0, TestWebp, "image/webp", "test", null, null,
                1, 1, DateTimeOffset.UtcNow, SafetyStatus: ImageSafetyStatus.Safe), default);
            word.SetImage($"local:{id}", "test", null);
        }
        var scheduler = new RecordingScheduler();
        var job = new WordImageBackfillJob(
            new FakeTopicRepository(topics), new FakeImageService(), store, TimeProvider.System,
            NullLogger<WordImageBackfillJob>.Instance, scheduler, TimeSpan.Zero);

        var first = await job.RunAsync();
        var second = await job.RunBatchAsync(100);

        first.WordsConsidered.Should().Be(0);
        first.AlreadyHadImage.Should().Be(100);
        second.Stored.Should().Be(20);
        scheduler.ContentJobs.Should().ContainSingle(jobExpression =>
            jobExpression.Contains("RunBatchAsync", StringComparison.Ordinal));
    }

    private sealed class FakeImageService(ImageSafetyStatus safetyStatus = ImageSafetyStatus.Safe) : IImageService
    {
        public List<string> Queries { get; } = [];

        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken)
        {
            Queries.Add(query);
            return Task.FromResult<DownloadedImage?>(new DownloadedImage(
                TestPng,
                "image/png",
                "Unsplash",
                "Photo by Test on Unsplash",
                "https://unsplash.com/photos/test",
                800,
                600,
                safetyStatus,
                "test",
                DateTimeOffset.UtcNow));
        }

        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query,
            int count,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DownloadedImage>>([]);
    }

    private sealed class FirstLookupFailsImageService : IImageService
    {
        private int _calls;

        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) == 1)
                return Task.FromResult<DownloadedImage?>(null);
            return Task.FromResult<DownloadedImage?>(new DownloadedImage(
                TestPng, "image/png", "test", null, null, 1, 1,
                ImageSafetyStatus.Safe, "test", DateTimeOffset.UtcNow));
        }

        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query, int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DownloadedImage>>([]);
    }

    private sealed class SemanticFallbackImageService : IImageService
    {
        public List<string> Queries { get; } = [];

        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken)
        {
            Queries.Add(query);
            return Task.FromResult<DownloadedImage?>(query == "run"
                ? new DownloadedImage(
                    TestPng, "image/png", "test", null, null, 1, 1,
                    ImageSafetyStatus.Safe, "test", DateTimeOffset.UtcNow)
                : null);
        }

        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query, int count, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DownloadedImage>>([]);
    }

    private sealed class RecordingScheduler : IBackgroundJobScheduler
    {
        public List<string> ContentJobs { get; } = [];

        public string EnqueueContent<TJob>(System.Linq.Expressions.Expression<Func<TJob, Task>> methodCall)
        {
            ContentJobs.Add(methodCall.Body.ToString());
            return Guid.NewGuid().ToString("N");
        }

        public string EnqueueMaintenance<TJob>(System.Linq.Expressions.Expression<Func<TJob, Task>> methodCall) =>
            Guid.NewGuid().ToString("N");
    }

    private sealed class FakeTopicRepository(params VocabularyTopic[] topics) : IVocabularyTopicRepository
    {
        private readonly List<VocabularyTopic> _topics = topics.ToList();

        public Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_topics.FirstOrDefault(topic => topic.Id == id));

        public Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.Where(topic => ids.Contains(topic.Id)).ToList());

        public Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(
            CefrLevel level,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.Where(topic => topic.Level == level).ToList());

        public Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.ToList());

        public Task SaveAsync(VocabularyTopic topic, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            _topics.RemoveAll(topic => topic.Id == id);
            return Task.CompletedTask;
        }
    }
}
