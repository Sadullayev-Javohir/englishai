namespace Application.Gamification.Dtos;

/// <summary>
/// The authoritative reward produced by one completed skill activity. Zero amounts mean the
/// activity did not qualify or that skill had already been rewarded today.
/// </summary>
public sealed record SkillRewardDto(
    int AwardedXp,
    int AwardedCoins,
    int ModulePoints,
    int DailyActivityBonus,
    int AllModulesBonus,
    int StreakMilestoneBonus,
    bool PremiumMultiplierApplied,
    int CurrentStreak,
    bool AlreadyCreditedToday)
{
    public static SkillRewardDto NotAwarded(bool alreadyCreditedToday = false, int currentStreak = 0) =>
        new(0, 0, 0, 0, 0, 0, false, currentStreak, alreadyCreditedToday);
}
