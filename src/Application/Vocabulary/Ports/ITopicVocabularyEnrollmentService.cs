using Domain.Vocabulary;

namespace Application.Vocabulary.Ports;

public sealed record TopicVocabularyEnrollmentResult(int ExistingCount, int InsertedCount);

public interface ITopicVocabularyEnrollmentService
{
    Task<TopicVocabularyEnrollmentResult> EnrollAsync(
        Guid learnerId,
        VocabularyTopic topic,
        DateTimeOffset learnedAt,
        CancellationToken cancellationToken);
}
