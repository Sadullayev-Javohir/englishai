using Application.Vocabulary.Ports;
using Domain.Vocabulary;

namespace Application.Vocabulary;

public sealed class TopicVocabularyEnrollmentService : ITopicVocabularyEnrollmentService
{
    private readonly IVocabularyRepository _vocabulary;

    public TopicVocabularyEnrollmentService(IVocabularyRepository vocabulary)
    {
        _vocabulary = vocabulary;
    }

    public async Task<TopicVocabularyEnrollmentResult> EnrollAsync(
        Guid learnerId,
        VocabularyTopic topic,
        DateTimeOffset learnedAt,
        CancellationToken cancellationToken)
    {
        if (topic.Words.Count == 0)
            return new TopicVocabularyEnrollmentResult(0, 0);

        var existing = await _vocabulary.GetByLearnerIdAsync(learnerId, cancellationToken);
        var known = new HashSet<string>(
            existing.Where(item => item.SourceTopicId == topic.Id).Select(item => item.Word),
            StringComparer.OrdinalIgnoreCase);
        var existingCount = known.Count;
        var insertedCount = 0;
        var pending = new List<VocabularyItem>();

        foreach (var word in topic.Words)
        {
            if (!known.Add(word.Word))
                continue;

            pending.Add(VocabularyItem.Learn(
                learnerId,
                word.Word,
                word.Translation,
                learnedAt,
                word.ExampleSentence,
                VocabularySource.Vocabulary,
                topic.Id,
                word.PartOfSpeech));
        }

        foreach (var item in pending)
        {
            await _vocabulary.SaveAsync(item, cancellationToken);
            insertedCount++;
        }

        return new TopicVocabularyEnrollmentResult(existingCount, insertedCount);
    }
}
