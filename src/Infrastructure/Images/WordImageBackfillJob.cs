using Application.Common;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace Infrastructure.Images;

public sealed record WordImageBackfillResult(
    int Topics,
    int WordsConsidered,
    int AlreadyHadImage,
    int Stored);

/// <summary>
/// Downloads one licensed, safety-checked image for every vocabulary word, stores the bytes locally,
/// and records a stable marker on the word. Runs in bounded batches and queues the next batch so the
/// full 300-topic/6,000-word catalogue can be filled without one oversized Hangfire execution.
/// </summary>
public sealed class WordImageBackfillJob
{
    private const int MaxWordsPerRun = 100;
    private const int MaxConcurrentDownloads = 6;
    private static readonly TimeSpan DefaultThrottleDelay = TimeSpan.FromMilliseconds(40);

    private readonly IVocabularyTopicRepository _topics;
    private readonly IImageService _images;
    private readonly ITopicImageStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<WordImageBackfillJob> _logger;
    private readonly IBackgroundJobScheduler? _jobs;
    private readonly TimeSpan _throttleDelay;

    public WordImageBackfillJob(
        IVocabularyTopicRepository topics,
        IImageService images,
        ITopicImageStore store,
        TimeProvider clock,
        ILogger<WordImageBackfillJob> logger,
        IBackgroundJobScheduler? jobs = null,
        TimeSpan? throttleDelay = null)
    {
        _topics = topics;
        _images = images;
        _store = store;
        _clock = clock;
        _logger = logger;
        _jobs = jobs;
        _throttleDelay = throttleDelay ?? DefaultThrottleDelay;
    }

    public async Task<WordImageBackfillResult> RunAsync(CancellationToken cancellationToken = default)
        => await RunBatchAsync(0, cancellationToken);

    public async Task<WordImageBackfillResult> RunBatchAsync(
        int offset,
        CancellationToken cancellationToken = default)
    {
        var topics = await _topics.GetAllAsync(cancellationToken);
        var filled = topics.Where(topic => topic.IsFilled).ToList();
        var words = filled
            .SelectMany(topic => topic.Words.Select(word => (Topic: topic, Word: word)))
            .Skip(Math.Max(0, offset))
            .Take(MaxWordsPerRun)
            .ToList();
        var pending = new List<(VocabularyTopic Topic, TopicWord Word, Guid ImageId)>();
        var alreadyHad = 0;

        foreach (var (topic, word) in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var imageId = WordImageQuery.ImageId(topic.Id, word.Word);
            var existing = await _store.GetByTopicIdAsync(imageId, cancellationToken);
            if (IsReusableWebp(existing)
                && word.ImageUrl?.Equals($"local:{imageId}", StringComparison.OrdinalIgnoreCase) == true)
            {
                alreadyHad++;
                continue;
            }

            if (IsReusableWebp(existing))
            {
                alreadyHad++;
                word.SetImage($"local:{imageId}", existing!.Source, existing.Attribution);
                await _topics.SaveAsync(topic, cancellationToken);
                continue;
            }

            if (existing is not null)
                await _store.DeleteAsync(imageId, 0, cancellationToken);

            pending.Add((topic, word, imageId));
        }

        var nextOffset = Math.Max(0, offset) + words.Count;
        if (pending.Count == 0)
        {
            if (words.Count == MaxWordsPerRun && _jobs is not null)
                _jobs.EnqueueContent<WordImageBackfillJob>(
                    job => job.RunBatchAsync(nextOffset, CancellationToken.None));
            return new WordImageBackfillResult(topics.Count, 0, alreadyHad, 0);
        }

        var resolved = new DownloadedImage?[pending.Count];
        using var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);
        await Task.WhenAll(pending.Select(async (item, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var primaryQuery = WordImageQuery.From(
                    item.Word.Word,
                    item.Word.Translation,
                    item.Word.PartOfSpeech,
                    item.Word.ExampleSentence);
                var fallbackQuery = WordImageQuery.From(item.Word.Word, item.Word.Translation);
                resolved[index] = await _images.DownloadImageAsync(primaryQuery, cancellationToken);
                if (resolved[index] is null
                    && !fallbackQuery.Equals(primaryQuery, StringComparison.OrdinalIgnoreCase))
                {
                    resolved[index] = await _images.DownloadImageAsync(fallbackQuery, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Word image lookup failed for {Word}.", item.Word.Word);
            }
            finally
            {
                semaphore.Release();
            }
        }));

        var changedTopics = new HashSet<VocabularyTopic>();
        var stored = 0;
        for (var index = 0; index < pending.Count; index++)
        {
            var image = resolved[index];
            if (image is null || image.SafetyStatus != ImageSafetyStatus.Safe)
                continue;

            var item = pending[index];
            var optimized = WebpConverter.ToWebp(image.Data, image.ContentType);
            ImageInfo? optimizedInfo;
            try
            {
                optimizedInfo = Image.Identify(optimized.Data);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Rejected undecodable image candidate from {Source} for {Word}.",
                    image.Source,
                    item.Word.Word);
                continue;
            }
            if (optimizedInfo is null)
                continue;
            await _store.SaveAsync(
                new TopicImageContent(
                    item.ImageId,
                    0,
                    optimized.Data,
                    optimized.ContentType,
                    image.Source,
                    image.Attribution,
                    image.SourceUrl,
                    optimizedInfo?.Width ?? image.Width,
                    optimizedInfo?.Height ?? image.Height,
                    _clock.GetUtcNow(),
                    SafetyStatus: image.SafetyStatus,
                    SafetyModelVersion: image.SafetyModelVersion,
                    SafetyCheckedAt: image.SafetyCheckedAt,
                    SafetyReasons: image.SafetyReasons),
                cancellationToken);

            if (item.Word.SetImage($"local:{item.ImageId}", image.Source, image.Attribution))
                changedTopics.Add(item.Topic);
            stored++;
        }

        foreach (var topic in changedTopics)
            await _topics.SaveAsync(topic, cancellationToken);

        if (_throttleDelay > TimeSpan.Zero)
            await Task.Delay(_throttleDelay, cancellationToken);

        _logger.LogInformation(
            "Word image backfill considered {Considered} words; stored {Stored}; {Existing} already safe.",
            pending.Count,
            stored,
            alreadyHad);

        if (words.Count == MaxWordsPerRun && _jobs is not null)
            _jobs.EnqueueContent<WordImageBackfillJob>(
                job => job.RunBatchAsync(nextOffset, CancellationToken.None));

        return new WordImageBackfillResult(topics.Count, pending.Count, alreadyHad, stored);
    }

    private static bool IsReusableWebp(TopicImageContent? image)
        => image is
        {
            SafetyStatus: ImageSafetyStatus.Safe,
            ContentType: "image/webp",
            Data.Length: >= 12,
        }
        && image.Data.AsSpan(0, 4).SequenceEqual("RIFF"u8)
        && image.Data.AsSpan(8, 4).SequenceEqual("WEBP"u8);
}
