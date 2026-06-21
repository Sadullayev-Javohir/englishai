using Application.Common;
using Application.Images.GetTopicImage;
using FluentAssertions;
using System.Runtime.CompilerServices;
using Xunit;

namespace Application.Tests.Images;

/// <summary>
/// Verifies the topic-image serving handler returns the stored bytes when an image exists and null
/// when it does not (so the endpoint replies 404 and the UI shows a placeholder).
/// </summary>
public class GetTopicImageQueryHandlerTests
{
    [Fact]
    public async Task Returns_stored_image_bytes()
    {
        var topicId = Guid.NewGuid();
        var store = new FakeTopicImageStore();
        await store.SaveAsync(Image(topicId, 0, 9, 8, 7), default);

        var result = await new GetTopicImageQueryHandler(store).Handle(new GetTopicImageQuery(topicId), default);

        result.Should().NotBeNull();
        result!.Data.Should().Equal(9, 8, 7);
        result.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task Returns_null_when_topic_has_no_image()
    {
        var store = new FakeTopicImageStore();

        var result = await new GetTopicImageQueryHandler(store).Handle(new GetTopicImageQuery(Guid.NewGuid()), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Strict_serving_hides_unapproved_image()
    {
        var topicId = Guid.NewGuid();
        var store = new FakeTopicImageStore();
        await store.SaveAsync(Image(topicId, 0, 1, 2, 3), default);

        var result = await new GetTopicImageQueryHandler(
            store,
            new ImageSafetyServingOptions { RequireSafetyApproval = true })
            .Handle(new GetTopicImageQuery(topicId), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Returns_the_requested_gallery_slot()
    {
        var topicId = Guid.NewGuid();
        var store = new FakeTopicImageStore();
        await store.SaveAsync(Image(topicId, 0, 1), default);
        await store.SaveAsync(Image(topicId, 2, 2, 2), default);

        var result = await new GetTopicImageQueryHandler(store).Handle(new GetTopicImageQuery(topicId, 2), default);

        result!.Data.Should().Equal(2, 2);
    }

    [Fact]
    public async Task Falls_back_to_cover_when_requested_slot_is_empty()
    {
        var topicId = Guid.NewGuid();
        var store = new FakeTopicImageStore();
        await store.SaveAsync(Image(topicId, 0, 5, 5), default);

        // Slot 3 has no image, so the cover (slot 0) is served instead of a 404.
        var result = await new GetTopicImageQueryHandler(store).Handle(new GetTopicImageQuery(topicId, 3), default);

        result!.Data.Should().Equal(5, 5);
    }

    private static TopicImageContent Image(Guid topicId, int slot, params byte[] data) =>
        new(topicId, slot, data, "image/png", "Unsplash", null, null, 1, 1, DateTimeOffset.UtcNow);

    private sealed class FakeTopicImageStore : ITopicImageStore
    {
        private readonly Dictionary<(Guid, int), TopicImageContent> _images = new();

        public Task<TopicImageContent?> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken) =>
            GetBySlotAsync(topicId, 0, cancellationToken);

        public Task<TopicImageContent?> GetBySlotAsync(Guid topicId, int slot, CancellationToken cancellationToken) =>
            Task.FromResult(_images.GetValueOrDefault((topicId, slot)));

        public Task<IReadOnlyList<TopicImageSlotInfo>> GetManifestAsync(
            Guid topicId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TopicImageSlotInfo>>(_images.Values
                .Where(i => i.TopicId == topicId)
                .OrderBy(i => i.Slot)
                .Select(i => new TopicImageSlotInfo(
                    i.Slot, i.Source, i.Attribution, i.SourceUrl, i.SafetyStatus))
                .ToList());

        public Task<IReadOnlyDictionary<Guid, int>> GetImageCountsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, int>>(_images.Keys
                .GroupBy(k => k.Item1)
                .ToDictionary(g => g.Key, g => g.Count()));

        public Task SaveAsync(TopicImageContent image, CancellationToken cancellationToken)
        {
            _images[(image.TopicId, image.Slot)] = image;
            return Task.CompletedTask;
        }

        public Task MarkSafetyAsync(
            Guid topicId,
            int slot,
            ImageSafetyStatus status,
            string? modelVersion,
            DateTimeOffset checkedAt,
            string? reasons,
            CancellationToken cancellationToken)
        {
            if (_images.TryGetValue((topicId, slot), out var image))
                _images[(topicId, slot)] = image with { SafetyStatus = status };
            return Task.CompletedTask;
        }

        public Task<int> DeleteAsync(Guid topicId, int slot, CancellationToken cancellationToken) =>
            Task.FromResult(_images.Remove((topicId, slot)) ? 1 : 0);

        public Task<ImageSafetyInventory> GetSafetyInventoryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ImageSafetyInventory(_images.Count, 0, _images.Count, 0, 0));

        public Task<int> DeleteAllAsync(CancellationToken cancellationToken)
        {
            var removed = _images.Count;
            _images.Clear();
            return Task.FromResult(removed);
        }

        public async IAsyncEnumerable<TopicImageContent> GetAllAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var image in _images.Values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return image;
            }
            await Task.CompletedTask;
        }
    }
}
