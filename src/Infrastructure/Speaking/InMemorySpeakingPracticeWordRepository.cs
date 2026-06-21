using System.Collections.Concurrent;
using Application.Speaking.Ports;
using Domain.Speaking;

namespace Infrastructure.Speaking;

public sealed class InMemorySpeakingPracticeWordRepository : ISpeakingPracticeWordRepository
{
    private readonly ConcurrentDictionary<Guid, SpeakingPracticeWord> _words = new();

    public Task<SpeakingPracticeWord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_words.TryGetValue(id, out var word) ? word : null);

    public Task<SpeakingPracticeWord?> GetByLearnerAndWordAsync(
        Guid learnerId, string normalizedWord, CancellationToken cancellationToken = default) =>
        Task.FromResult(_words.Values.FirstOrDefault(word =>
            word.LearnerId == learnerId && word.NormalizedWord == normalizedWord));

    public Task<IReadOnlyList<SpeakingPracticeWord>> GetActiveAsync(
        Guid learnerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SpeakingPracticeWord> result = _words.Values
            .Where(word => word.LearnerId == learnerId && word.IsActive)
            .OrderByDescending(word => word.LastFailedAt)
            .ThenBy(word => word.Word)
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveAsync(SpeakingPracticeWord word, CancellationToken cancellationToken = default)
    {
        _words[word.Id] = word;
        return Task.CompletedTask;
    }
}
