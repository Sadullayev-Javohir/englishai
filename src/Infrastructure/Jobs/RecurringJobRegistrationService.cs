using Hangfire;
using Infrastructure.Assistant.Sessions;
using Infrastructure.Notifications;
using Infrastructure.Retention;
using Infrastructure.Storage;
using Infrastructure.Subscription;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Jobs;

public sealed class RecurringJobRegistrationService(
    IRecurringJobManager recurringJobs) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var tashkent = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent");

        recurringJobs.AddOrUpdate<DueReviewNotificationJob>(
            "srs-due-reviews", HangfireQueues.Notifications, job => job.RunAsync(), Cron.Daily(9),
            new RecurringJobOptions { TimeZone = tashkent });
        recurringJobs.AddOrUpdate<DailyPlanNotificationJob>(
            "daily-plan-streak-risk", HangfireQueues.Notifications, job => job.RunAsync(), Cron.Daily(20),
            new RecurringJobOptions { TimeZone = tashkent });
        recurringJobs.AddOrUpdate<SubscriptionExpiryJob>(
            "subscription-expiry", HangfireQueues.Critical, job => job.RunAsync(), Cron.Daily(9),
            new RecurringJobOptions { TimeZone = tashkent });
        recurringJobs.AddOrUpdate<RetentionSweepJob>(
            "retention-winback", HangfireQueues.Notifications, job => job.RunAsync(), Cron.Daily(9),
            new RecurringJobOptions { TimeZone = tashkent });
        // Later in the morning than the other notifications so a trial reminder does not arrive in
        // the same burst as the daily study nudges and get skimmed past.
        recurringJobs.AddOrUpdate<Infrastructure.Identity.TrialExpiryJob>(
            "trial-expiry", HangfireQueues.Notifications, job => job.RunAsync(), Cron.Daily(11),
            new RecurringJobOptions { TimeZone = tashkent });
        recurringJobs.AddOrUpdate<AssistantSessionCleanupJob>(
            "assistant-session-cleanup", HangfireQueues.Maintenance, job => job.RunAsync(), Cron.Hourly(),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        recurringJobs.AddOrUpdate<MediaObjectMigrationJob>(
            "media-object-migration", HangfireQueues.Maintenance,
            job => job.RunAsync(100, CancellationToken.None), Cron.Hourly(),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
