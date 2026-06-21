using Application.Gamification;
using Application.Gamification.CompleteTask;
using Application.Gamification.GetGamificationStatus;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Tests.Learning;
using Domain.Gamification;
using Domain.Identity;
using Domain.Learning;
using FluentAssertions;
using Xunit;
using GoalTarget = Domain.Gamification.DailyGoal;
using GoalPreference = Domain.Identity.DailyGoal;

namespace Application.Tests.Gamification;

public class GamificationHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 6, 22);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly FakeGamificationStore _store = new();
    private readonly FakeUserPreferencesStore _preferences = new();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    [Fact]
    public async Task The_first_completed_task_starts_the_streak_before_the_goal_is_met()
    {
        var handler = new CompleteTaskCommandHandler(_store, _preferences, _clock);

        var status = await handler.Handle(new CompleteTaskCommand(Learner), CancellationToken.None);

        status.TodayCompletedTasks.Should().Be(1);
        status.DailyGoalTarget.Should().Be(GoalTarget.DefaultTargetTasks);
        // One task is enough to keep the streak alive, even though the full daily goal is not met.
        status.IsGoalMet.Should().BeFalse();
        status.CurrentStreak.Should().Be(1);
        _store.CompletedDays(Learner).Should().Contain(Today);
    }

    [Fact]
    public async Task Reaching_the_daily_goal_flips_the_goal_indicator_while_the_streak_holds()
    {
        var handler = new CompleteTaskCommandHandler(_store, _preferences, _clock);

        for (var i = 0; i < GoalTarget.DefaultTargetTasks; i++)
            await handler.Handle(new CompleteTaskCommand(Learner), CancellationToken.None);

        var status = await handler.Handle(new CompleteTaskCommand(Learner), CancellationToken.None);

        status.IsGoalMet.Should().BeTrue();
        status.CurrentStreak.Should().Be(1);
        status.IsStreakAtRisk.Should().BeFalse(); // not at risk once today is done
        _store.CompletedDays(Learner).Should().Contain(Today);
    }

    [Fact]
    public async Task Extra_tasks_after_the_goal_keep_the_day_completed_once()
    {
        var handler = new CompleteTaskCommandHandler(_store, _preferences, _clock);

        for (var i = 0; i < GoalTarget.DefaultTargetTasks + 2; i++)
            await handler.Handle(new CompleteTaskCommand(Learner), CancellationToken.None);

        _store.CompletedDays(Learner).Should().ContainSingle().Which.Should().Be(Today);
    }

    [Fact]
    public async Task Status_query_reports_an_at_risk_streak_from_yesterday()
    {
        // Yesterday was completed, today has no tasks yet.
        await _store.MarkDayCompletedAsync(Learner, Today.AddDays(-1), CancellationToken.None);
        var handler = new GetGamificationStatusQueryHandler(_store, _preferences, _clock);

        var status = await handler.Handle(new GetGamificationStatusQuery(Learner), CancellationToken.None);

        status.TodayCompletedTasks.Should().Be(0);
        status.CurrentStreak.Should().Be(1);
        status.IsStreakAtRisk.Should().BeTrue();
        status.IsGoalMet.Should().BeFalse();
    }

    [Fact]
    public async Task An_intense_daily_goal_preference_raises_the_target_and_delays_goal_met()
    {
        _preferences.Set(PreferencesWith(GoalPreference.Intense));
        var handler = new CompleteTaskCommandHandler(_store, _preferences, _clock);

        // The default (3) would already be met here, but Intense demands 5.
        for (var i = 0; i < DailyGoalPreference.NormalTargetTasks; i++)
            await handler.Handle(new CompleteTaskCommand(Learner), CancellationToken.None);
        var status = await handler.Handle(new CompleteTaskCommand(Learner), CancellationToken.None);

        status.DailyGoalTarget.Should().Be(DailyGoalPreference.IntenseTargetTasks);
        status.IsGoalMet.Should().BeFalse(); // 4 done, needs 5
    }

    [Fact]
    public async Task A_calm_daily_goal_preference_lowers_the_target()
    {
        _preferences.Set(PreferencesWith(GoalPreference.Calm));
        var handler = new GetGamificationStatusQueryHandler(_store, _preferences, _clock);

        var status = await handler.Handle(new GetGamificationStatusQuery(Learner), CancellationToken.None);

        status.DailyGoalTarget.Should().Be(DailyGoalPreference.CalmTargetTasks);
    }

    private static UserPreferences PreferencesWith(GoalPreference goal)
    {
        var prefs = UserPreferences.CreateDefault(Learner, Now);
        prefs.Update(goal, LanguageBalance.Bilingual, true, true, Now);
        return prefs;
    }

    private sealed class FakeUserPreferencesStore : IUserPreferencesStore
    {
        private readonly Dictionary<Guid, UserPreferences> _prefs = new();

        public void Set(UserPreferences preferences) => _prefs[preferences.UserId] = preferences;

        public Task<UserPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(_prefs.TryGetValue(userId, out var p) ? p : null);

        public Task SaveAsync(UserPreferences preferences, CancellationToken cancellationToken)
        {
            _prefs[preferences.UserId] = preferences;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGamificationStore : IGamificationStore
    {
        private readonly Dictionary<(Guid, DateOnly), int> _counts = new();
        private readonly Dictionary<Guid, HashSet<DateOnly>> _days = new();
        private readonly Dictionary<(Guid, DateOnly), HashSet<SkillType>> _skills = new();

        public IReadOnlyCollection<DateOnly> CompletedDays(Guid learnerId) =>
            _days.TryGetValue(learnerId, out var set) ? set : Array.Empty<DateOnly>();

        public Task<IReadOnlyCollection<SkillType>> MarkSkillPracticedAsync(
            Guid learnerId, DateOnly day, SkillType skill, CancellationToken cancellationToken)
        {
            if (!_skills.TryGetValue((learnerId, day), out var set))
                _skills[(learnerId, day)] = set = new HashSet<SkillType>();
            set.Add(skill);
            return Task.FromResult<IReadOnlyCollection<SkillType>>(set.ToArray());
        }

        public Task<IReadOnlyCollection<SkillType>> GetSkillsPracticedTodayAsync(
            Guid learnerId, DateOnly day, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<SkillType>>(
                _skills.TryGetValue((learnerId, day), out var set) ? set.ToArray() : Array.Empty<SkillType>());

        public Task<int> IncrementTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
        {
            _counts.TryGetValue((learnerId, day), out var current);
            current++;
            _counts[(learnerId, day)] = current;
            return Task.FromResult(current);
        }

        public Task<int> GetTaskCountAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
        {
            _counts.TryGetValue((learnerId, day), out var current);
            return Task.FromResult(current);
        }

        public Task MarkDayCompletedAsync(Guid learnerId, DateOnly day, CancellationToken cancellationToken)
        {
            if (!_days.TryGetValue(learnerId, out var set))
                _days[learnerId] = set = new HashSet<DateOnly>();
            set.Add(day);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<DateOnly>> GetCompletedDaysAsync(
            Guid learnerId, CancellationToken cancellationToken) =>
            Task.FromResult(CompletedDays(learnerId));

        public Task DeleteLearnerAsync(Guid learnerId, CancellationToken cancellationToken)
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
