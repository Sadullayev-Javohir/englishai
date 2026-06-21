using Application.Common;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Speaking;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Verifies the topic-image backfill with fakes: <see cref="TopicImageBackfillJob"/> downloads a
/// gallery per pending topic plus one cover per roleplay scenario (keyed by the scenario's
/// deterministic ImageId), skips galleries that are already full (idempotent, re-runnable), and
/// leaves a gallery empty when the provider returns nothing (so a later run retries it).
/// </summary>
public class TopicImageBackfillJobTests
{
    private static VocabularyTopic NewTopic(string slug, string title) =>
        VocabularyTopic.Curate(slug, title, title + " uz", "Life", "present_simple", CefrLevel.A1, DateTimeOffset.UtcNow);

    private static TopicImageBackfillJob BuildJob(
        FakeTopicRepository topics, IImageService images, ITopicImageStore store) =>
        new(topics, images, store, TimeProvider.System, NullLogger<TopicImageBackfillJob>.Instance,
            throttleDelay: TimeSpan.Zero); // fake providers need no rate-limit pause

    private const int Target = TopicImageBackfillJob.TargetImagesPerTopic;

    // Every run also tops up the 120 roleplay scenario covers (1 image each).
    private const int Scenarios = RoleplayScenarioCatalog.TotalScenarios;

    // …and the 120 free-talk topic covers (1 image each).
    private const int FreeTalkTopics = FreeTalkTopicCatalog.TotalTopics;

    // Non-vocabulary galleries topped up on every run (roleplay + free-talk), 1 cover each.
    private const int CuratedCovers = Scenarios + FreeTalkTopics;

    [Fact]
    public async Task Downloads_and_stores_a_full_gallery_per_pending_topic()
    {
        var a = NewTopic("a1-family", "My Family");
        var b = NewTopic("a1-food", "Food");
        var topics = new FakeTopicRepository(a, b);
        var store = new InMemoryTopicImageStore();

        var result = await BuildJob(topics, new FakeImageService(), store).RunAsync();

        result.Topics.Should().Be(2 + CuratedCovers);
        result.Downloaded.Should().Be(2 * Target + CuratedCovers);

        // The cover (slot 0) and every gallery slot are stored.
        var cover = await store.GetByTopicIdAsync(a.Id, default);
        cover.Should().NotBeNull();
        cover!.Slot.Should().Be(0);
        cover.ContentType.Should().Be("image/jpeg");
        cover.Source.Should().Be("Unsplash");

        (await store.GetBySlotAsync(a.Id, Target - 1, default)).Should().NotBeNull();
        (await store.GetManifestAsync(a.Id, default)).Should().HaveCount(Target);

        // Every roleplay scenario got its cover, stored under the scenario's deterministic ImageId.
        var scenario = RoleplayScenarioCatalog.Get("airport");
        (await store.GetByTopicIdAsync(scenario.ImageId, default)).Should().NotBeNull();

        // …and every free-talk topic got its cover, keyed by the topic's deterministic ImageId.
        var freeTalk = FreeTalkTopicCatalog.All.First();
        (await store.GetByTopicIdAsync(freeTalk.ImageId, default)).Should().NotBeNull();
    }

    [Fact]
    public async Task Is_idempotent_so_a_second_run_downloads_nothing()
    {
        var topics = new FakeTopicRepository(NewTopic("a1-family", "My Family"));
        var images = new FakeImageService();
        var store = new InMemoryTopicImageStore();
        var job = BuildJob(topics, images, store);

        await job.RunAsync();
        var second = await job.RunAsync();

        second.Downloaded.Should().Be(0);
        second.AlreadyHadImage.Should().Be(1 + CuratedCovers);
        // The provider was hit once per gallery on the first run only (topic + every curated cover).
        images.Calls.Should().Be(1 + CuratedCovers);
    }

    [Fact]
    public async Task Tops_up_a_partial_gallery_on_a_later_run()
    {
        var topic = NewTopic("a1-family", "My Family");
        var topics = new FakeTopicRepository(topic);
        var store = new InMemoryTopicImageStore();

        // First run: provider yields only 2 images (rate-limited), leaving a partial gallery.
        await BuildJob(topics, new FakeImageService(maxPerCall: 2), store).RunAsync();
        (await store.GetManifestAsync(topic.Id, default)).Should().HaveCount(2);

        // Second run: provider recovers and fills the remaining slots, contiguously.
        var second = await BuildJob(topics, new FakeImageService(), store).RunAsync();

        second.Downloaded.Should().Be(Target - 2);
        var manifest = await store.GetManifestAsync(topic.Id, default);
        manifest.Should().HaveCount(Target);
        manifest.Select(m => m.Slot).Should().BeEquivalentTo(Enumerable.Range(0, Target));
    }

    [Fact]
    public async Task Leaves_topic_image_less_when_no_image_is_available()
    {
        var topic = NewTopic("a1-family", "My Family");
        var topics = new FakeTopicRepository(topic);
        var store = new InMemoryTopicImageStore();

        var result = await BuildJob(topics, new FakeImageService(returnsNull: true), store).RunAsync();

        result.Downloaded.Should().Be(0);
        (await store.GetByTopicIdAsync(topic.Id, default)).Should().BeNull();
    }

    private sealed class FakeImageService : IImageService
    {
        private readonly bool _returnsNull;
        private readonly int _maxPerCall;
        public int Calls { get; private set; }

        public FakeImageService(bool returnsNull = false, int maxPerCall = int.MaxValue)
        {
            _returnsNull = returnsNull;
            _maxPerCall = maxPerCall;
        }

        public Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<ImageResult?>(null);

        public Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_returnsNull ? null : NewImage(0));
        }

        public Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
            string query, int count, CancellationToken cancellationToken)
        {
            Calls++;
            if (_returnsNull)
                return Task.FromResult<IReadOnlyList<DownloadedImage>>(Array.Empty<DownloadedImage>());

            var n = Math.Min(count, _maxPerCall);
            var images = Enumerable.Range(0, n).Select(i => NewImage(i)!).ToList();
            return Task.FromResult<IReadOnlyList<DownloadedImage>>(images);
        }

        private static DownloadedImage NewImage(int i) => new(
            new byte[] { 0xFF, 0xD8, 0xFF, (byte)i }, "image/jpeg", "Unsplash", "Photo by Test on Unsplash",
            $"https://unsplash.com/photo/{i}", 800, 600,
            ImageSafetyStatus.Safe, "test", DateTimeOffset.UtcNow);
    }

    private sealed class FakeTopicRepository : IVocabularyTopicRepository
    {
        private readonly List<VocabularyTopic> _topics;
        public FakeTopicRepository(params VocabularyTopic[] topics) => _topics = topics.ToList();

        public Task<VocabularyTopic?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_topics.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<VocabularyTopic>> GetByIdsAsync(
            IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.Where(t => ids.Contains(t.Id)).ToList());

        public Task<IReadOnlyList<VocabularyTopic>> GetByLevelAsync(CefrLevel level, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.Where(t => t.Level == level).ToList());

        public Task SaveAsync(VocabularyTopic topic, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<VocabularyTopic>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VocabularyTopic>>(_topics.ToList());

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            _topics.RemoveAll(t => t.Id == id);
            return Task.CompletedTask;
        }
    }
}
