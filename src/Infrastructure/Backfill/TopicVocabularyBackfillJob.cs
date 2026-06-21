using Application.Vocabulary.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Backfill;

public sealed record TopicVocabularyBackfillResult(
    int RecordsScanned,
    int WordsExisting,
    int WordsInserted,
    int Failures);

public sealed class TopicVocabularyBackfillJob
{
    private readonly ITopicCompletionStore _completions;
    private readonly IVocabularyTopicRepository _topics;
    private readonly ITopicVocabularyEnrollmentService _enrollment;
    private readonly TimeProvider _clock;
    private readonly ILogger<TopicVocabularyBackfillJob> _logger;

    public TopicVocabularyBackfillJob(
        ITopicCompletionStore completions,
        IVocabularyTopicRepository topics,
        ITopicVocabularyEnrollmentService enrollment,
        TimeProvider clock,
        ILogger<TopicVocabularyBackfillJob> logger)
    {
        _completions = completions;
        _topics = topics;
        _enrollment = enrollment;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TopicVocabularyBackfillResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var records = await _completions.GetWithVocabularyProgressAsync(cancellationToken);
        if (records.Count == 0)
            return new TopicVocabularyBackfillResult(0, 0, 0, 0);

        var topicIds = records.Select(record => record.VocabularyTopicId).Distinct().ToArray();
        var topics = (await _topics.GetByIdsAsync(topicIds, cancellationToken))
            .ToDictionary(topic => topic.Id);
        var learnedAt = _clock.GetUtcNow();
        var existing = 0;
        var inserted = 0;
        var failures = 0;

        foreach (var record in records)
        {
            if (!topics.TryGetValue(record.VocabularyTopicId, out var topic))
            {
                failures++;
                _logger.LogWarning(
                    "Topic vocabulary backfill skipped missing topic {TopicId} for learner {LearnerId}.",
                    record.VocabularyTopicId, record.LearnerId);
                continue;
            }

            try
            {
                var result = await _enrollment.EnrollAsync(
                    record.LearnerId, topic, learnedAt, cancellationToken);
                existing += result.ExistingCount;
                inserted += result.InsertedCount;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failures++;
                _logger.LogError(
                    exception,
                    "Topic vocabulary backfill failed for topic {TopicId}, learner {LearnerId}.",
                    record.VocabularyTopicId, record.LearnerId);
            }
        }

        var summary = new TopicVocabularyBackfillResult(records.Count, existing, inserted, failures);
        _logger.LogInformation(
            "Topic vocabulary backfill: scanned {RecordsScanned}, existing {WordsExisting}, inserted {WordsInserted}, failures {Failures}.",
            summary.RecordsScanned, summary.WordsExisting, summary.WordsInserted, summary.Failures);
        return summary;
    }
}
