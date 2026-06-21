using Application.Analytics.Dtos;
using Application.Analytics.Ports;
using Application.Identity.Ports;
using Application.Subscription.Ports;
using Domain.Analytics;
using MediatR;

namespace Application.Analytics.GetPublicSocialProofMetrics;

public sealed class GetPublicSocialProofMetricsQueryHandler
    : IRequestHandler<GetPublicSocialProofMetricsQuery, PublicSocialProofMetricsDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IStudyLogStore _studyLogs;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;

    public GetPublicSocialProofMetricsQueryHandler(
        IUserAccountStore accounts,
        ISubscriptionRepository subscriptions,
        IStudyLogStore studyLogs,
        IProductEventStore productEvents,
        TimeProvider clock)
    {
        _accounts = accounts;
        _subscriptions = subscriptions;
        _studyLogs = studyLogs;
        _productEvents = productEvents;
        _clock = clock;
    }

    public async Task<PublicSocialProofMetricsDto> Handle(
        GetPublicSocialProofMetricsQuery request,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var activityFrom = today.AddDays(-29);

        // EF Core does not allow multiple concurrent operations on the same scoped DbContext.
        // Keep these reads sequential; the endpoint caches the aggregate for ten minutes.
        var accounts = await _accounts.GetAllAsync(cancellationToken);
        var accountIds = accounts.Select(account => account.Id).ToHashSet();
        var subscriptions = await _subscriptions.GetPaidAsync(cancellationToken);
        var activity = await _studyLogs.GetActivityDaysAsync(activityFrom, cancellationToken);
        var studyTotals = await _studyLogs.GetGlobalTotalsAsync(cancellationToken);
        var productEvents = await _productEvents.GetAllAsync(cancellationToken);

        var activePremiumUsers = subscriptions
            .Where(subscription => accountIds.Contains(subscription.LearnerId) && subscription.IsPremiumActive(now))
            .Select(subscription => subscription.LearnerId)
            .Distinct()
            .Count();

        var activeLearners30d = activity
            .Where(day => accountIds.Contains(day.LearnerId))
            .Select(day => day.LearnerId)
            .Distinct()
            .Count();

        var speakingSessions = productEvents
            .Where(productEvent =>
                productEvent.Type == ProductEventType.SpeakingSessionCompleted &&
                accountIds.Contains(productEvent.LearnerId))
            .Select(productEvent => new
            {
                productEvent.LearnerId,
                Session = productEvent.Source ?? productEvent.Id.ToString(),
            })
            .Distinct()
            .LongCount();

        return new PublicSocialProofMetricsDto(
            AsOf: today,
            RegisteredUsers: accounts.Count,
            ActivePremiumUsers: activePremiumUsers,
            ActiveLearners30d: activeLearners30d,
            TotalStudyMinutes: studyTotals.TotalSeconds / 60,
            SpeakingSessions: speakingSessions,
            SpeakingMinutes: studyTotals.SpeakingSeconds / 60);
    }
}
