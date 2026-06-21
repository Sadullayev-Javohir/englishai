using Domain.Vocabulary;

namespace Application.Vocabulary.Ports;

public interface IVocabularyStatsReader
{
    Task<VocabularyStats> ReadAsync(Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken);
}
