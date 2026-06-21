using Application.Analytics.GetStudyStats;
using Application.Common;
using Application.Gamification.GetGamificationStatus;
using Application.Gamification.GetPointsBalance;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Learning.GetGrowth;
using Application.Learning.GetLearnerOverview;
using Application.Levels.GetLevelMap;
using Application.Subscription.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Learning.GetPublicLearnerProgress;

public sealed class GetPublicLearnerProgressQueryHandler
    : IRequestHandler<GetPublicLearnerProgressQuery, PublicLearnerProgressDto>
{
    private readonly ISender _sender;
    private readonly IUserAccountStore _accounts;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ILeaderboardStore _leaderboard;
    private readonly TimeProvider _clock;

    public GetPublicLearnerProgressQueryHandler(
        ISender sender,
        IUserAccountStore accounts,
        ISubscriptionRepository subscriptions,
        ILeaderboardStore leaderboard,
        TimeProvider clock)
    {
        _sender = sender;
        _accounts = accounts;
        _subscriptions = subscriptions;
        _leaderboard = leaderboard;
        _clock = clock;
    }

    public async Task<PublicLearnerProgressDto> Handle(
        GetPublicLearnerProgressQuery request,
        CancellationToken cancellationToken)
    {
        // This is an intentionally cross-learner read (see IOwnershipExempt on the query).
        // The projection is built from several learner-scoped sub-queries dispatched with the
        // *target* learner's id while the caller's token still belongs to the viewer, so bypass
        // ownership for the duration of the fan-out - otherwise each sub-query is rejected as a
        // "you can only access your own data" 403.
        using var _ = OwnershipBypass.Enter();

        var account = await _accounts.GetByIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.LearnerId);
        var overview = await _sender.Send(new GetLearnerOverviewQuery(request.LearnerId), cancellationToken);
        var studyStats = await _sender.Send(
            new GetStudyStatsQuery(request.LearnerId, request.Today), cancellationToken);
        var growth = await _sender.Send(new GetGrowthQuery(request.LearnerId), cancellationToken);
        var gamification = await _sender.Send(
            new GetGamificationStatusQuery(request.LearnerId), cancellationToken);
        var levelMap = await _sender.Send(
            new GetLevelMapQuery(request.LearnerId, overview.OverallLevel), cancellationToken);
        var points = await _sender.Send(new GetPointsBalanceQuery(request.LearnerId), cancellationToken);
        var rank = await _leaderboard.GetLearnerRankAsync(
            request.LearnerId, overview.OverallLevel, cancellationToken);
        var subscription = await _subscriptions.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        var now = _clock.GetUtcNow();
        var isPremium = account.IsProTrialActive(now) || subscription?.IsPremiumActive(now) == true;

        return new PublicLearnerProgressDto(
            request.LearnerId,
            account.DisplayName,
            account.PictureUrl,
            isPremium,
            points.LifetimeXp,
            rank?.Rank,
            overview.OverallLevel,
            overview.SkillScores,
            levelMap.TopicsTotal,
            levelMap.TopicsLearned,
            levelMap.TopicsMastered,
            studyStats,
            gamification,
            growth);
    }
}
