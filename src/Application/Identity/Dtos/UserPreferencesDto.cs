using Domain.Identity;

namespace Application.Identity.Dtos;

/// <summary>The learner's account preferences as exposed to (and saved from) the Profile screen.</summary>
public sealed record UserPreferencesDto(
    DailyGoal DailyGoal,
    LanguageBalance LanguageBalance,
    bool EmailNotifications,
    bool PushNotifications)
{
    public static UserPreferencesDto From(UserPreferences preferences) =>
        new(preferences.DailyGoal, preferences.LanguageBalance,
            preferences.EmailNotifications, preferences.PushNotifications);

    /// <summary>The defaults to surface when the learner has never saved a preference.</summary>
    public static UserPreferencesDto Default() =>
        new(DailyGoal.Normal, LanguageBalance.Bilingual, true, true);
}
