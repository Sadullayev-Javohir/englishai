using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Domain.Gamification;
using MediatR;

namespace Application.Gamification.CompleteTask;

public sealed class CompleteTaskCommandHandler : IRequestHandler<CompleteTaskCommand, GamificationStatusDto>
{
    private readonly IGamificationStore _store;
    private readonly IUserPreferencesStore _preferences;
    private readonly TimeProvider _clock;

    public CompleteTaskCommandHandler(
        IGamificationStore store, IUserPreferencesStore preferences, TimeProvider clock)
    {
        _store = store;
        _preferences = preferences;
        _clock = clock;
    }

    public async Task<GamificationStatusDto> Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var goal = await DailyGoalPreference.ResolveAsync(_preferences, request.LearnerId, cancellationToken);
        // Credit the task to the learner's local day (UTC+5), matching the daily-plan rollover.
        var today = _clock.LocalToday();

        var todayCount = await _store.IncrementTaskCountAsync(request.LearnerId, today, cancellationToken);

        // Any completed task keeps today's streak alive (the widely-expected "did something today"
        // streak); the daily goal (three tasks) stays a separate indicator via IsGoalMet.
        // MarkDayCompletedAsync is idempotent, so re-completing tasks the same day is harmless.
        await _store.MarkDayCompletedAsync(request.LearnerId, today, cancellationToken);

        var completedDays = await _store.GetCompletedDaysAsync(request.LearnerId, cancellationToken);
        var streak = StreakCalculator.Compute(completedDays, today);

        return GamificationStatusDto.From(todayCount, goal, streak);
    }
}
