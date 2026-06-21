using Application.Identity.Dtos;
using Domain.Identity;
using MediatR;

namespace Application.Identity.UpdateUserPreferences;

/// <summary>Saves the learner's account preferences from the Profile screen.</summary>
public sealed record UpdateUserPreferencesCommand(
    Guid UserId,
    DailyGoal DailyGoal,
    LanguageBalance LanguageBalance,
    bool EmailNotifications,
    bool PushNotifications) : IRequest<UserPreferencesDto>;
