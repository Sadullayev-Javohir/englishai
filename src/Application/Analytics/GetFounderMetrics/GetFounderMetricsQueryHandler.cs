using Application.Ai;
using Application.Analytics.Dtos;
using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Subscription.Ports;
using MediatR;

namespace Application.Analytics.GetFounderMetrics;

/// <summary>
/// Assembles the founder growth dashboard. Authorizes the viewer as an admin (403 otherwise - same
/// gate as the admin panel), then bulk-reads accounts, learner profiles, paid subscriptions and the
/// recent study-activity days and hands them to the pure <see cref="FounderMetricsCalculator"/>. The
/// reads are whole-table scans keyed into dictionaries by the calculator (no per-user round-trip),
/// which is appropriate for an operator-facing report at startup scale (mirrors GetAdminUsers).
/// </summary>
public sealed class GetFounderMetricsQueryHandler : IRequestHandler<GetFounderMetricsQuery, FounderMetricsDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IStudyLogStore _studyLog;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;
    private readonly IVariableCostMeter? _costs;

    public GetFounderMetricsQueryHandler(
        IAdminAuthorization admin,
        IUserAccountStore accounts,
        ILearnerProfileRepository profiles,
        ISubscriptionRepository subscriptions,
        IStudyLogStore studyLog,
        IProductEventStore productEvents,
        TimeProvider clock,
        IVariableCostMeter? costs = null)
    {
        _admin = admin;
        _accounts = accounts;
        _profiles = profiles;
        _subscriptions = subscriptions;
        _studyLog = studyLog;
        _productEvents = productEvents;
        _clock = clock;
        _costs = costs;
    }

    public async Task<FounderMetricsDto> Handle(GetFounderMetricsQuery request, CancellationToken cancellationToken)
    {
        var viewerRole = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (viewerRole is AdminRole.None)
            throw new ForbiddenException("Admin access is required.");

        var now = _clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var accounts = await _accounts.GetAllAsync(cancellationToken);
        var earliestAccountDay = accounts.Count > 0
            ? accounts.Min(account => DateOnly.FromDateTime(account.CreatedAt.UtcDateTime))
            : today;
        var trendTo = request.To is { } requestedTo && requestedTo < today ? requestedTo : today;
        var trendFrom = request.From ?? trendTo.AddDays(-(FounderMetricsCalculator.TrendWindowDays - 1));
        if (trendFrom > trendTo)
            trendFrom = trendTo;
        if (request.From is null && request.To is null)
            trendFrom = trendTo.AddDays(-(FounderMetricsCalculator.TrendWindowDays - 1));
        else if (request.From == DateOnly.MinValue)
            trendFrom = earliestAccountDay;

        var retentionFrom = today.AddDays(-FounderMetricsCalculator.RetentionLookbackDays);
        var activityFrom = trendFrom < retentionFrom ? trendFrom : retentionFrom;

        var profiles = await _profiles.GetAllAsync(cancellationToken);
        var paidSubscriptions = await _subscriptions.GetPaidAsync(cancellationToken);
        var activityDays = await _studyLog.GetActivityDaysAsync(activityFrom, cancellationToken);
        var productEvents = await _productEvents.GetAllAsync(cancellationToken);

        return FounderMetricsCalculator.Build(
            today, now, accounts, profiles, paidSubscriptions, activityDays, productEvents, trendFrom, trendTo) with
        {
            VariableCosts = _costs?.Snapshot(),
            CanManageVariableCostBudget = viewerRole is AdminRole.SuperAdmin,
        };
    }
}
