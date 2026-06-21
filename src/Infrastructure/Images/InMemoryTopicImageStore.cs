using System.Collections.Concurrent;
using Application.Common;
using Infrastructure.Storage;

namespace Infrastructure.Images;

/// <summary>
/// In-memory <see cref="ITopicImageStore"/> used in dev/tests (no database). Holds each topic's
/// gallery (keyed by topic id + slot) for the process lifetime; the EF adapter is used when a
/// database is configured.
/// </summary>
public sealed class InMemoryTopicImageStore : ITopicImageStore
{
    private readonly ConcurrentDictionary<(Guid TopicId, int Slot), TopicImageContent> _images = new();

    public Task<TopicImageContent?> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken) =>
        GetBySlotAsync(topicId, 0, cancellationToken);

    public Task<TopicImageContent?> GetBySlotAsync(Guid topicId, int slot, CancellationToken cancellationToken) =>
        Task.FromResult(_images.TryGetValue((topicId, slot), out var image) ? image : null);

    public Task<IReadOnlyList<TopicImageSlotInfo>> GetManifestAsync(
        Guid topicId, CancellationToken cancellationToken)
    {
        var slots = _images.Values
            .Where(i => i.TopicId == topicId)
            .OrderBy(i => i.Slot)
            .Select(i => new TopicImageSlotInfo(
                i.Slot, i.Source, i.Attribution, i.SourceUrl, i.SafetyStatus))
            .ToList();
        return Task.FromResult<IReadOnlyList<TopicImageSlotInfo>>(slots);
    }

    public Task<IReadOnlyDictionary<Guid, int>> GetImageCountsAsync(CancellationToken cancellationToken)
    {
        var counts = _images
            .Where(pair => pair.Value.SafetyStatus == ImageSafetyStatus.Safe)
            .Select(pair => pair.Key)
            .GroupBy(k => k.TopicId)
            .ToDictionary(g => g.Key, g => g.Count());
        return Task.FromResult<IReadOnlyDictionary<Guid, int>>(counts);
    }

    public Task SaveAsync(TopicImageContent image, CancellationToken cancellationToken)
    {
        if (image.Data is null) throw new InvalidDataException("Topic image content is required.");
        MediaValidation.EnsureSize(image.Data.LongLength, MediaValidation.MaxTopicImageBytes);
        if (!MediaValidation.DetectImage(image.Data).Equals(image.ContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Topic image MIME type does not match its content.");
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
        {
            _images[(topicId, slot)] = image with
            {
                SafetyStatus = status,
                SafetyModelVersion = modelVersion,
                SafetyCheckedAt = checkedAt,
                SafetyReasons = reasons,
            };
        }
        return Task.CompletedTask;
    }

    public Task<int> DeleteAllAsync(CancellationToken cancellationToken)
    {
        var removed = _images.Count;
        _images.Clear();
        return Task.FromResult(removed);
    }

    public Task<int> DeleteAsync(Guid topicId, int slot, CancellationToken cancellationToken) =>
        Task.FromResult(_images.TryRemove((topicId, slot), out _) ? 1 : 0);

    public Task<ImageSafetyInventory> GetSafetyInventoryAsync(CancellationToken cancellationToken)
    {
        var values = _images.Values.ToArray();
        return Task.FromResult(new ImageSafetyInventory(
            values.Length,
            values.Count(image => image.SafetyStatus == ImageSafetyStatus.Safe),
            values.Count(image => image.SafetyStatus == ImageSafetyStatus.Pending),
            values.Count(image => image.SafetyStatus == ImageSafetyStatus.Unsafe),
            values.Count(image => image.SafetyStatus == ImageSafetyStatus.Failed)));
    }

    public async IAsyncEnumerable<TopicImageContent> GetAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var image in _images.Values.OrderBy(i => i.TopicId).ThenBy(i => i.Slot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return image;
        }
        await Task.CompletedTask;
    }
}
