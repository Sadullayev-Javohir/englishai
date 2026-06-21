using System.Collections.Concurrent;
using Application.Gamification.Ports;
using Domain.Learning;

namespace Infrastructure.Gamification;

/// <summary>
/// Dev/test <see cref="IGamificationStore"/> mirroring the Redis layout: a per-(learner,
/// day) task counter and a per-learner set of completed days. Process-local and resets on
/// restart - fine for dev/tests; the Redis adapter is the durable production path
/// (docs/development-guide.md rule 10 - external services behind ports).
/// </summary>
public sealed class InMemoryGamificationStore : IGamificationStore
{
    private readonly ConcurrentDictionary<(Guid, DateOnly), int> _counts = new();
    private readonly ConcurrentDictionary<Guid, HashSet<DateOnly>> _completedDays = new();
    private readonly ConcurrentDictionary<(Guid, DateOnly), HashSet<SkillType>> _skills = new();

    public Task<int> IncrementTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
    {
        var count = _counts.AddOrUpdate((learnerId, day), 1, (_, current) => current + 1);
        return Task.FromResult(count);
    }

    public Task<int> GetTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
    {
        _counts.TryGetValue((learnerId, day), out var count);
        return Task.FromResult(count);
    }

    public Task<IReadOnlyCollection<SkillType>> MarkSkillPracticedAsync(
        Guid learnerId, DateOnly day, SkillType skill, CancellationToken cancellationToken)
    {
        var set = _skills.GetOrAdd((learnerId, day), _ => new HashSet<SkillType>());
        lock (set)
        {
            set.Add(skill);
            IReadOnlyCollection<SkillType> snapshot = set.ToArray();
            return Task.FromResult(snapshot);
        }
    }

    public Task<IReadOnlyCollection<SkillType>> GetSkillsPracticedTodayAsync(
        Guid learnerId, DateOnly day, CancellationToken cancellationToken)
    {
        if (!_skills.TryGetValue((learnerId, day), out var set))
            return Task.FromResult<IReadOnlyCollection<SkillType>>(Array.Empty<SkillType>());

        lock (set)
        {
            IReadOnlyCollection<SkillType> snapshot = set.ToArray();
            return Task.FromResult(snapshot);
        }
    }

    public Task MarkDayCompletedAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
    {
        var set = _completedDays.GetOrAdd(learnerId, _ => new HashSet<DateOnly>());
        lock (set)
        {
            set.Add(day);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<DateOnly>> GetCompletedDaysAsync(
        Guid learnerId, CancellationToken cancellationToken)
    {
        if (!_completedDays.TryGetValue(learnerId, out var set))
            return Task.FromResult<IReadOnlyCollection<DateOnly>>(Array.Empty<DateOnly>());

        lock (set)
        {
            IReadOnlyCollection<DateOnly> snapshot = set.ToArray();
            return Task.FromResult(snapshot);
        }
    }

    public Task DeleteLearnerAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        _completedDays.TryRemove(learnerId, out _);
        foreach (var key in _counts.Keys.Where(k => k.Item1 == learnerId).ToArray())
            _counts.TryRemove(key, out _);
        foreach (var key in _skills.Keys.Where(k => k.Item1 == learnerId).ToArray())
            _skills.TryRemove(key, out _);

        return Task.CompletedTask;
    }
}
