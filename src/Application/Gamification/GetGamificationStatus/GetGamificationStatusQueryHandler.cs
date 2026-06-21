using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Domain.Gamification;
using MediatR;

namespace Application.Gamification.GetGamificationStatus;

public sealed class GetGamificationStatusQueryHandler
    : IRequestHandler<GetGamificationStatusQuery, GamificationStatusDto>
{
    private readonly IGamificationStore _store;
    private readonly IUserPreferencesStore _preferences;
    private readonly TimeProvider _clock;

    public GetGamificationStatusQueryHandler(
        IGamificationStore store, IUserPreferencesStore preferences, TimeProvider clock)
    {
        _store = store;
        _preferences = preferences;
        _clock = clock;
    }

    public async Task<GamificationStatusDto> Handle(
        GetGamificationStatusQuery request, CancellationToken cancellationToken)
    {
        var goal = await DailyGoalPreference.ResolveAsync(_preferences, request.LearnerId, cancellationToken);
        // The daily plan rolls over at the learner's local midnight (UTC+5), not UTC midnight.
        var today = _clock.LocalToday();

        var todayCount = await _store.GetTaskCountAsync(request.LearnerId, today, cancellationToken);
        var completedDays = await _store.GetCompletedDaysAsync(request.LearnerId, cancellationToken);
        var streak = StreakCalculator.Compute(completedDays, today);
        var skillsToday = await _store.GetSkillsPracticedTodayAsync(request.LearnerId, today, cancellationToken);

        return GamificationStatusDto.From(todayCount, goal, streak, skillsToday);
    }
}
