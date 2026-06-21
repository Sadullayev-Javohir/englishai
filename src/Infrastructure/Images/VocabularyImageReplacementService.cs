using Application.Common;
using Application.Vocabulary.Admin.Images;
using Application.Vocabulary.Ports;
using Domain.Common;
using Infrastructure.Persistence;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;

namespace Infrastructure.Images;

public sealed class VocabularyImageReplacementService : IVocabularyImageReplacementService
{
    private const int CandidateCount = 8;
    private static readonly TimeSpan ProviderSearchTimeout = TimeSpan.FromSeconds(4);

    private readonly IVocabularyTopicRepository _topics;
    private readonly IImageService _images;
    private readonly ITopicImageStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<VocabularyImageReplacementService> _logger;
    private readonly EnglishAiDbContext? _db;

    public VocabularyImageReplacementService(
        IVocabularyTopicRepository topics,
        IImageService images,
        ITopicImageStore store,
        TimeProvider clock,
        ILogger<VocabularyImageReplacementService> logger,
        EnglishAiDbContext? db = null)
    {
        _topics = topics;
        _images = images;
        _store = store;
        _clock = clock;
        _logger = logger;
        _db = db;
    }

    public async Task<VocabularyImageAdminDto> ReplaceAsync(
        Guid topicId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(topicId, cancellationToken)
            ?? throw new NotFoundException("VocabularyTopic", topicId);
        var word = topic.Words.FirstOrDefault(candidate =>
            WordImageQuery.ImageId(topic.Id, candidate.Word) == imageId)
            ?? throw new NotFoundException("Vocabulary word image", imageId);

        var current = await _store.GetByTopicIdAsync(imageId, cancellationToken);
        var query = WordImageQuery.From(
            word.Word,
            word.Translation,
            word.PartOfSpeech,
            word.ExampleSentence);
        IReadOnlyList<DownloadedImage> candidates;
        using (var providerTimeout = new CancellationTokenSource(ProviderSearchTimeout))
        using (var providerCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                   cancellationToken,
                   providerTimeout.Token))
        {
            try
            {
                candidates = await _images.DownloadImagesAsync(
                    query,
                    CandidateCount,
                    providerCancellation.Token);
            }
            catch (OperationCanceledException) when (
                providerTimeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Vocabulary image provider search timed out after {TimeoutSeconds}s for {Word}; using generated safe WebP.",
                    ProviderSearchTimeout.TotalSeconds,
                    word.Word);
                candidates = [];
            }
        }
        var selected = candidates.FirstOrDefault(candidate =>
            candidate.SafetyStatus == ImageSafetyStatus.Safe
            && !SameImage(current, candidate));
        // Photo search can legitimately return no distinct safe hit for abstract or uncommon words.
        // Never strand the admin: create a safe local WebP that visibly contains the exact English
        // word and its Uzbek meaning. The timestamp makes repeated replacements distinct.
        selected ??= GeneratedVocabularyImageFactory.Create(
            word.Word,
            word.Translation,
            _clock.GetUtcNow().ToUnixTimeMilliseconds());

        var optimized = WebpConverter.ToWebp(selected.Data, selected.ContentType);
        if (!optimized.ContentType.Equals(WebpConverter.WebpContentType, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("Yangi rasmni WebP formatiga aylantirib bo‘lmadi.");

        var info = Image.Identify(optimized.Data)
            ?? throw new DomainException("Yangi rasm fayli o‘qilmadi.");
        var now = _clock.GetUtcNow();
        await using var transaction = await BeginTransactionAsync(cancellationToken);
        try
        {
            await _store.SaveAsync(
                new TopicImageContent(
                    imageId,
                    0,
                    optimized.Data,
                    optimized.ContentType,
                    selected.Source,
                    selected.Attribution,
                    selected.SourceUrl,
                    info.Width,
                    info.Height,
                    now,
                    SafetyStatus: selected.SafetyStatus,
                    SafetyModelVersion: selected.SafetyModelVersion,
                    SafetyCheckedAt: selected.SafetyCheckedAt,
                    SafetyReasons: selected.SafetyReasons),
                cancellationToken);

            word.SetImage($"local:{imageId}", selected.Source, selected.Attribution);
            await _topics.SaveAsync(topic, cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        _logger.LogInformation(
            "Admin replaced vocabulary image {ImageId} for {Word} from {Source}.",
            imageId,
            word.Word,
            selected.Source);

        return new VocabularyImageAdminDto(
            topic.Id,
            imageId,
            topic.Title,
            topic.Level.ToString(),
            word.Word,
            word.Translation,
            $"/api/images/vocabulary-topics/{topic.Id}/words/{imageId}",
            selected.Source,
            selected.Attribution,
            now.ToUnixTimeMilliseconds());
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_db is null || _db.Database.CurrentTransaction is not null)
            return null;
        return await _db.Database.BeginTransactionAsync(cancellationToken);
    }

    private static bool SameImage(TopicImageContent? current, DownloadedImage candidate)
    {
        if (current?.Data is null)
            return false;
        if (!string.IsNullOrWhiteSpace(current.SourceUrl)
            && string.Equals(current.SourceUrl, candidate.SourceUrl, StringComparison.OrdinalIgnoreCase))
            return true;
        return ObjectKeys.Checksum(current.Data) == ObjectKeys.Checksum(candidate.Data);
    }
}
