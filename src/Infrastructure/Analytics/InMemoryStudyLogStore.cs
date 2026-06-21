using System.Collections.Concurrent;
using Application.Analytics.Ports;
using Domain.Analytics;
using Domain.Learning;

namespace Infrastructure.Analytics;

/// <summary>
/// Dev/test <see cref="IStudyLogStore"/> - a process-local map of (learner, day) study records.
/// Resets on restart; the EF adapter is the durable production path.
/// </summary>
public sealed class InMemoryStudyLogStore : IStudyLogStore
{
    private readonly ConcurrentDictionary<(Guid Learner, DateOnly Day), DailyStudyRecord> _records = new();
    private readonly object _gate = new();

    public Task AddStudyTimeAsync(
        Guid learnerId, DateOnly day, SkillType skill, int seconds, CancellationToken cancellationToken)
    {
        // The record is mutated in place, so guard the get-or-create + AddTime against concurrent
        // heartbeats for the same (learner, day).
        lock (_gate)
        {
            var record = _records.GetOrAdd((learnerId, day), _ => DailyStudyRecord.Start(learnerId, day));
            record.AddTime(skill, seconds);
        }

        return Task.CompletedTask;
    }

    public Task<StudyStats> GetStatsAsync(
        Guid learnerId, DateOnly today, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(StudyStatsCalculator.Compute(
                _records.Values.Where(r => r.LearnerId == learnerId), today));
        }
    }

    public Task<IReadOnlyList<StudyActivityDay>> GetActivityDaysAsync(
        DateOnly fromDay, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<StudyActivityDay> all = _records.Values
                .Where(r => r.Day >= fromDay)
                .Select(r => new StudyActivityDay(r.LearnerId, r.Day))
                .ToList();
            return Task.FromResult(all);
        }
    }

    public Task<GlobalStudyTotals> GetGlobalTotalsAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var records = _records.Values.ToList();
            return Task.FromResult(new GlobalStudyTotals(
                records.Sum(record => (long)record.TotalSeconds),
                records.Sum(record => (long)record.SpeakingSeconds)));
        }
    }
}
