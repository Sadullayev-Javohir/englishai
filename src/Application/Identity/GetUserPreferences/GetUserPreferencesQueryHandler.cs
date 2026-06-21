using Application.Identity.Dtos;
using Application.Identity.Ports;
using MediatR;

namespace Application.Identity.GetUserPreferences;

public sealed class GetUserPreferencesQueryHandler
    : IRequestHandler<GetUserPreferencesQuery, UserPreferencesDto>
{
    private readonly IUserPreferencesStore _preferences;

    public GetUserPreferencesQueryHandler(IUserPreferencesStore preferences)
    {
        _preferences = preferences;
    }

    public async Task<UserPreferencesDto> Handle(
        GetUserPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        // Never persist a default just to read it - a learner who has not changed anything
        // simply sees the defaults until they save.
        var preferences = await _preferences.GetByUserIdAsync(request.UserId, cancellationToken);
        return preferences is null ? UserPreferencesDto.Default() : UserPreferencesDto.From(preferences);
    }
}
