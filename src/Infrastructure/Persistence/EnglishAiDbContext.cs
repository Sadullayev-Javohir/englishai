using Domain.Analytics;
using Domain.Assistant;
using Domain.Books;
using Domain.Curriculum;
using Domain.Developer;
using Domain.Grammar;
using Domain.Identity;
using Domain.Learning;
using Domain.Listening;
using Domain.Reading;
using Domain.Retention;
using Domain.Speaking;
using Domain.Subscription;
using Domain.Video;
using Domain.Vocabulary;
using Domain.Writing;
using Domain.Support;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

/// <summary>
/// Application database context (PostgreSQL). The schema grows phase by phase as
/// durable persistence is introduced (LearnerProfile in Faza 2,
/// VocabularyItem/ReviewSchedule in Faza 3, VideoLesson in Faza 4,
/// Subscription/Payment in Faza 5, ReadingPassage/GrammarLesson/WritingTask in Faza 6, ...).
/// </summary>
public class EnglishAiDbContext : DbContext
{
    public EnglishAiDbContext(DbContextOptions<EnglishAiDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<AssistantSession> AssistantSessions => Set<AssistantSession>();
    public DbSet<AssistantMessage> AssistantMessages => Set<AssistantMessage>();
    public DbSet<SupportConversation> SupportConversations => Set<SupportConversation>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<SupportAttachment> SupportAttachments => Set<SupportAttachment>();
    public DbSet<DeveloperApiKey> DeveloperApiKeys => Set<DeveloperApiKey>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<LearnerProfile> LearnerProfiles => Set<LearnerProfile>();
    public DbSet<VocabularyItem> VocabularyItems => Set<VocabularyItem>();
    public DbSet<VocabularyTopic> VocabularyTopics => Set<VocabularyTopic>();
    public DbSet<TopicSpeakingProgress> TopicSpeakingProgress => Set<TopicSpeakingProgress>();
    public DbSet<SpeakingPracticeWord> SpeakingPracticeWords => Set<SpeakingPracticeWord>();
    public DbSet<TopicCompletionRecord> TopicCompletionRecords => Set<TopicCompletionRecord>();
    public DbSet<VideoLesson> VideoLessons => Set<VideoLesson>();
    public DbSet<Domain.Subscription.Subscription> Subscriptions => Set<Domain.Subscription.Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PremiumWaitlistEntry> PremiumWaitlistEntries => Set<PremiumWaitlistEntry>();
    public DbSet<ReadingPassage> ReadingPassages => Set<ReadingPassage>();
    public DbSet<ListeningExercise> ListeningExercises => Set<ListeningExercise>();
    public DbSet<Infrastructure.Listening.ListeningAudioClip> ListeningAudioClips
        => Set<Infrastructure.Listening.ListeningAudioClip>();
    public DbSet<Infrastructure.Images.TopicImage> TopicImages
        => Set<Infrastructure.Images.TopicImage>();
    public DbSet<Infrastructure.Identity.Avatar.UserAvatar> UserAvatars
        => Set<Infrastructure.Identity.Avatar.UserAvatar>();
    public DbSet<GrammarLesson> GrammarLessons => Set<GrammarLesson>();
    public DbSet<WritingTask> WritingTasks => Set<WritingTask>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookProgress> BookProgress => Set<BookProgress>();
    public DbSet<DailyStudyRecord> DailyStudyRecords => Set<DailyStudyRecord>();
    public DbSet<ProductEvent> ProductEvents => Set<ProductEvent>();
    public DbSet<Domain.Referral.ReferralAccount> ReferralAccounts => Set<Domain.Referral.ReferralAccount>();
    public DbSet<Domain.Referral.Referral> Referrals => Set<Domain.Referral.Referral>();
    public DbSet<Domain.Notifications.NotificationReadState> NotificationReadStates
        => Set<Domain.Notifications.NotificationReadState>();
    public DbSet<Domain.Notifications.AdminBroadcast> AdminBroadcasts
        => Set<Domain.Notifications.AdminBroadcast>();
    public DbSet<Domain.Notifications.NotificationArrival> NotificationArrivals
        => Set<Domain.Notifications.NotificationArrival>();
    public DbSet<Domain.Notifications.NotificationDismissal> NotificationDismissals
        => Set<Domain.Notifications.NotificationDismissal>();
    public DbSet<Domain.Notifications.DeviceToken> DeviceTokens
        => Set<Domain.Notifications.DeviceToken>();
    public DbSet<Domain.Notifications.Notification> Notifications
        => Set<Domain.Notifications.Notification>();
    public DbSet<Domain.Gamification.LearnerPoints> LearnerPoints => Set<Domain.Gamification.LearnerPoints>();
    public DbSet<Domain.Gamification.EnergySpend> EnergySpends => Set<Domain.Gamification.EnergySpend>();
    public DbSet<Domain.Gamification.DiscountRedemption> DiscountRedemptions
        => Set<Domain.Gamification.DiscountRedemption>();
    public DbSet<Domain.Competition.Competition> Competitions => Set<Domain.Competition.Competition>();
    public DbSet<CurriculumGenerationRun> CurriculumGenerationRuns => Set<CurriculumGenerationRun>();
    public DbSet<CurriculumGenerationItem> CurriculumGenerationItems => Set<CurriculumGenerationItem>();
    public DbSet<TopicSpeakingBlueprint> TopicSpeakingBlueprints => Set<TopicSpeakingBlueprint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EnglishAiDbContext).Assembly);
    }
}
