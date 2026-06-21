using Application.Gamification;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Referral;
using Domain.Gamification;
using Domain.Learning;
using Domain.Referral;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Gamification;

public class DailyProgressRecorderTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 26, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private readonly FakeGamificationStore _store = new();
    private readonly DailyProgressRecorder _recorder;

    public DailyProgressRecorderTests() =>
        _recorder = new DailyProgressRecorder(_store, new NoOpReferralService(), new NoOpPointsService());

    [Fact]
    public async Task Records_the_skill_as_practiced_today()
    {
        var learner = Guid.NewGuid();

        await _recorder.RecordSkillAsync(learner, SkillType.Vocabulary, 80, Now, CancellationToken.None);

        var skills = await _store.GetSkillsPracticedTodayAsync(learner, Today, CancellationToken.None);
        skills.Should().Contain(SkillType.Vocabulary);
        (await _store.GetTaskCountAsync(learner, Today, CancellationToken.None)).Should().Be(1);
    }

    [Fact]
    public async Task Repeating_the_same_skill_does_not_inflate_the_daily_count()
    {
        var learner = Guid.NewGuid();

        await _recorder.RecordSkillAsync(learner, SkillType.Reading, 80, Now, CancellationToken.None);
        await _recorder.RecordSkillAsync(learner, SkillType.Reading, 80, Now, CancellationToken.None);

        (await _store.GetTaskCountAsync(learner, Today, CancellationToken.None)).Should().Be(1);
    }

    [Fact]
    public async Task Marks_the_day_active_from_the_first_completed_skill()
    {
        var learner = Guid.NewGuid();

        // A single completed activity keeps the streak alive - the day counts immediately.
        await _recorder.RecordSkillAsync(learner, SkillType.Vocabulary, 80, Now, CancellationToken.None);

        (await _store.GetCompletedDaysAsync(learner, CancellationToken.None)).Should().Contain(Today);
    }

    [Fact]
    public async Task An_unidentified_learner_records_nothing()
    {
        await _recorder.RecordSkillAsync(Guid.Empty, SkillType.Writing, 80, Now, CancellationToken.None);

        (await _store.GetTaskCountAsync(Guid.Empty, Today, CancellationToken.None)).Should().Be(0);
    }

    /// <summary>No-op points service: point awarding is a separate concern from streak recording.</summary>
    private sealed class NoOpPointsService : IPointsService
    {
        public Task<SkillRewardDto> AwardForActivityAsync(
            Guid learnerId, int scorePercent, IReadOnlyCollection<SkillType> skillsPracticedBeforeToday,
            DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(SkillRewardDto.NotAwarded());
    }

    /// <summary>No-op referral service: qualification is a separate concern from streak recording.</summary>
    private sealed class NoOpReferralService : IReferralService
    {
        public Task<ReferralAccount> GetOrCreateAccountAsync(Guid learnerId, CancellationToken cancellationToken) =>
            Task.FromResult(ReferralAccount.Create(learnerId, "ABCDEF", DateTimeOffset.UtcNow));

        public Task CaptureAsync(Guid refereeId, string? code, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task TryQualifyAsync(Guid refereeId, DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    /// <summary>Minimal stateful store (Application.Tests does not reference Infrastructure).</summary>
    private sealed class FakeGamificationStore : IGamificationStore
    {
        private readonly Dictionary<(Guid, DateOnly), int> _counts = new();
        private readonly Dictionary<(Guid, DateOnly), HashSet<SkillType>> _skills = new();
        private readonly Dictionary<Guid, HashSet<DateOnly>> _days = new();

        public Task<int> IncrementTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken ct)
        {
            _counts.TryGetValue((learnerId, day), out var c);
            _counts[(learnerId, day)] = ++c;
            return Task.FromResult(c);
        }

        public Task<int> GetTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken ct)
        {
            _counts.TryGetValue((learnerId, day), out var c);
            return Task.FromResult(c);
        }

        public Task<IReadOnlyCollection<SkillType>> MarkSkillPracticedAsync(
            Guid learnerId, DateOnly day, SkillType skill, CancellationToken ct)
        {
            if (!_skills.TryGetValue((learnerId, day), out var set))
                _skills[(learnerId, day)] = set = new HashSet<SkillType>();
            set.Add(skill);
            return Task.FromResult<IReadOnlyCollection<SkillType>>(set.ToArray());
        }

        public Task<IReadOnlyCollection<SkillType>> GetSkillsPracticedTodayAsync(
            Guid learnerId, DateOnly day, CancellationToken ct)
        {
            _skills.TryGetValue((learnerId, day), out var set);
            return Task.FromResult<IReadOnlyCollection<SkillType>>(
                set?.ToArray() ?? Array.Empty<SkillType>());
        }

        public Task MarkDayCompletedAsync(Guid learnerId, DateOnly day, CancellationToken ct)
        {
            if (!_days.TryGetValue(learnerId, out var set))
                _days[learnerId] = set = new HashSet<DateOnly>();
            set.Add(day);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<DateOnly>> GetCompletedDaysAsync(Guid learnerId, CancellationToken ct)
        {
            _days.TryGetValue(learnerId, out var set);
            return Task.FromResult<IReadOnlyCollection<DateOnly>>(
                set?.ToArray() ?? Array.Empty<DateOnly>());
        }

        public Task DeleteLearnerAsync(Guid learnerId, CancellationToken ct)
        {
            _days.Remove(learnerId);
            foreach (var key in _counts.Keys.Where(k => k.Item1 == learnerId).ToArray())
                _counts.Remove(key);
            foreach (var key in _skills.Keys.Where(k => k.Item1 == learnerId).ToArray())
                _skills.Remove(key);
            return Task.CompletedTask;
        }
    }
}
