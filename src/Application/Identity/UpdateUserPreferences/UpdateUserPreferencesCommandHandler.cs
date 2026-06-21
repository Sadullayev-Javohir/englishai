using Application.Identity.Dtos;
using Application.Identity.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Identity.UpdateUserPreferences;

public sealed class UpdateUserPreferencesCommandHandler
    : IRequestHandler<UpdateUserPreferencesCommand, UserPreferencesDto>
{
    private readonly IUserPreferencesStore _preferences;
    private readonly TimeProvider _clock;

    public UpdateUserPreferencesCommandHandler(IUserPreferencesStore preferences, TimeProvider clock)
    {
        _preferences = preferences;
        _clock = clock;
    }

    public async Task<UserPreferencesDto> Handle(
        UpdateUserPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();

        // Create-on-first-save, then mutate: preferences only ever exist once a learner opts in.
        var preferences = await _preferences.GetByUserIdAsync(request.UserId, cancellationToken)
                          ?? UserPreferences.CreateDefault(request.UserId, now);

        preferences.Update(
            request.DailyGoal,
            request.LanguageBalance,
            request.EmailNotifications,
            request.PushNotifications,
            now);

        await _preferences.SaveAsync(preferences, cancellationToken);
        return UserPreferencesDto.From(preferences);
    }
}
