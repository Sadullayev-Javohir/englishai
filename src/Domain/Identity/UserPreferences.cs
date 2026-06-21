namespace Domain.Identity;

/// <summary>How much the learner aims to study each day (drives the daily goal target).</summary>
public enum DailyGoal
{
    Calm = 1,
    Normal = 2,
    Intense = 3,
}

/// <summary>
/// How much Uzbek vs English the UI/feedback should lean on (docs/development-guide.md §11 adaptive balance,
/// here as an explicit user override of the level-derived default).
/// </summary>
public enum LanguageBalance
{
    Uzbek = 1,
    Bilingual = 2,
    English = 3,
}

/// <summary>
/// A learner's durable account preferences (settings shown on the Profile screen). Keyed by
/// <see cref="UserId"/>, which is the account id and therefore also the learner id. Created
/// lazily with sensible defaults the first time the learner saves a setting.
/// </summary>
public sealed class UserPreferences
{
    // Parameterless ctor for EF Core materialization.
    private UserPreferences()
    {
    }

    private UserPreferences(
        Guid userId,
        DailyGoal dailyGoal,
        LanguageBalance languageBalance,
        bool emailNotifications,
        bool pushNotifications,
        DateTimeOffset now)
    {
        UserId = userId;
        DailyGoal = dailyGoal;
        LanguageBalance = languageBalance;
        EmailNotifications = emailNotifications;
        PushNotifications = pushNotifications;
        UpdatedAt = now;
    }

    public Guid UserId { get; private set; }
    public DailyGoal DailyGoal { get; private set; }
    public LanguageBalance LanguageBalance { get; private set; }
    public bool EmailNotifications { get; private set; }
    public bool PushNotifications { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The defaults a freshly onboarded learner sees before changing anything.</summary>
    public static UserPreferences CreateDefault(Guid userId, DateTimeOffset now) =>
        new(userId, DailyGoal.Normal, LanguageBalance.Bilingual, true, true, now);

    public void Update(
        DailyGoal dailyGoal,
        LanguageBalance languageBalance,
        bool emailNotifications,
        bool pushNotifications,
        DateTimeOffset now)
    {
        DailyGoal = dailyGoal;
        LanguageBalance = languageBalance;
        EmailNotifications = emailNotifications;
        PushNotifications = pushNotifications;
        UpdatedAt = now;
    }
}
