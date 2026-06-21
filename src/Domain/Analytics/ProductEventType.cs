namespace Domain.Analytics;

/// <summary>
/// Product milestones used by the founder activation funnel. Keep this enum stable: values are stored
/// durably and the frontend sends the numeric value for client-observed signals (viewed/clicked).
/// </summary>
public enum ProductEventType
{
    Registered = 1,
    SignedIn = 2,
    UsernameSetupCompleted = 3,
    PlacementStarted = 4,
    PlacementCompleted = 5,
    TopicOpened = 6,
    SpeakingSessionCompleted = 7,
    PronunciationFeedbackViewed = 8,
    PaywallHit = 9,
    UpgradeClicked = 10,
    PaymentCompleted = 11,

    /// <summary>The learner picked their onboarding learning goal (goal-based onboarding funnel).</summary>
    OnboardingGoalSelected = 12,
}
