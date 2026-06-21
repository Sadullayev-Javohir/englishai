namespace Application.Notifications;

/// <summary>
/// Stable template codes for notifications. Each maps to a vetted Uzbek template in the
/// content layer (docs/development-guide.md rule 11). Kept here so command handlers reference codes, not
/// magic strings.
/// </summary>
public static class NotificationCodes
{
    /// <summary>A single word is due today; fills <c>{word}</c>.</summary>
    public const string ReviewOne = "notify.review_one";

    /// <summary>Several words are due today; fills <c>{count}</c>.</summary>
    public const string ReviewMany = "notify.review_many";

    /// <summary>A learned word's 3-day SRS checkpoint is due; fills <c>{word}</c> (PROJECT-SPEC B.1).</summary>
    public const string ReviewDueDay3 = "notify.review_due_day3";

    /// <summary>A learned word's 7-day SRS checkpoint is due; fills <c>{word}</c> (PROJECT-SPEC B.1).</summary>
    public const string ReviewDueDay7 = "notify.review_due_day7";

    /// <summary>A learned word's 21-day SRS checkpoint is due; fills <c>{word}</c> (PROJECT-SPEC B.1).</summary>
    public const string ReviewDueDay21 = "notify.review_due_day21";

    /// <summary>
    /// Several of a topic's words are due for review; fills <c>{topic}</c> (its Uzbek title) and
    /// <c>{count}</c>. All of a topic's due words (any 3/7/21-day stage) collapse into this one
    /// reminder so the feed never shows one row per word (PROJECT-SPEC principle #4).
    /// </summary>
    public const string ReviewDueTopic = "notify.review_due_topic";

    /// <summary>
    /// Due words that don't belong to a topic (manual/speaking/video additions); fills <c>{count}</c>.
    /// One grouped reminder covering all of them.
    /// </summary>
    public const string ReviewDueGeneral = "notify.review_due_general";

    /// <summary>The in-app destination an SRS review reminder deep-links to.</summary>
    public const string ReviewLinkUrl = "/vocabulary/review";

    /// <summary>
    /// The daily "learn English" reminder shown every day from 08:00 local time. Deep-links to Home.
    /// </summary>
    public const string DailyLearn = "notify.daily_learn";

    /// <summary>Premium ends soon; fills <c>{days}</c> (PROJECT-SPEC H.3).</summary>
    public const string SubscriptionExpiringSoon = "notify.subscription_expiring_soon";

    /// <summary>Premium has ended (PROJECT-SPEC H.3).</summary>
    public const string SubscriptionExpired = "notify.subscription_expired";

    /// <summary>
    /// The free Pro trial ends soon and checkout is OPEN; fills <c>{days}</c>. Sent only when a
    /// payment provider is connected - telling a learner to upgrade when there is nothing to buy is
    /// the fastest way to teach them the app's notifications are not worth reading.
    /// </summary>
    public const string TrialExpiringSoon = "notify.trial_expiring_soon";

    /// <summary>
    /// The free Pro trial ends soon and checkout is CLOSED; fills <c>{days}</c>. Points at the
    /// pricing page's waitlist instead of an upgrade that cannot be completed.
    /// </summary>
    public const string TrialExpiringWaitlist = "notify.trial_expiring_waitlist";

    /// <summary>The free Pro trial has ended.</summary>
    public const string TrialExpired = "notify.trial_expired";

    // --- Daily activity reminders (kunlik reja) ---------------------------------------------
    // Derived once per local day from the learner's gamification "skills practiced today" state.
    // One umbrella reminder when nothing is done yet, otherwise a per-skill nudge for each of the
    // six core skills still left in today's plan (PROJECT-SPEC Faza 5 - daily goal & majburlash).

    /// <summary>Nothing practiced yet today - start the daily plan. Deep-links to Home.</summary>
    public const string DailyPlan = "notify.daily_plan";

    /// <summary>20:00 reminder for an incomplete daily plan and at-risk streak.</summary>
    public const string DailyPlanStreakRisk = "notify.daily_plan_streak_risk";

    /// <summary>Vocabulary not practiced today.</summary>
    public const string PracticeVocabulary = "notify.practice_vocabulary";

    /// <summary>Grammar not practiced today.</summary>
    public const string PracticeGrammar = "notify.practice_grammar";

    /// <summary>Writing not practiced today.</summary>
    public const string PracticeWriting = "notify.practice_writing";

    /// <summary>Speaking not practiced today.</summary>
    public const string PracticeSpeaking = "notify.practice_speaking";

    /// <summary>Listening not practiced today.</summary>
    public const string PracticeListening = "notify.practice_listening";

    /// <summary>Reading not practiced today.</summary>
    public const string PracticeReading = "notify.practice_reading";

    /// <summary>
    /// A super-admin broadcast. Unlike the templated codes, its text is authored by the operator, so
    /// it carries its own <c>Title</c>/<c>Message</c> rather than resolving from the content store.
    /// </summary>
    public const string AdminBroadcast = "notify.admin_broadcast";

    /// <summary>In-app destinations the daily reminders deep-link to.</summary>
    public const string HomeLinkUrl = "/home";
    public const string VocabularyLinkUrl = "/vocabulary/topics";
    public const string GrammarLinkUrl = "/grammar";
    public const string WritingLinkUrl = "/writing";
    public const string SpeakingLinkUrl = "/speaking";
    public const string ListeningLinkUrl = "/listening";
    public const string ReadingLinkUrl = "/reading";
}
