using Domain.Speaking;

namespace Application.Speaking.Ports;

public interface ISpeakingPracticeWordRepository
{
    Task<SpeakingPracticeWord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SpeakingPracticeWord?> GetByLearnerAndWordAsync(
        Guid learnerId, string normalizedWord, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpeakingPracticeWord>> GetActiveAsync(
        Guid learnerId, CancellationToken cancellationToken = default);
    Task SaveAsync(SpeakingPracticeWord word, CancellationToken cancellationToken = default);
}
