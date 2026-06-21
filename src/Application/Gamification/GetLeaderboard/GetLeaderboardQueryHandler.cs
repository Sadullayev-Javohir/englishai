using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Subscription.Ports;
using Domain.Assessment;
using MediatR;

namespace Application.Gamification.GetLeaderboard;

public sealed class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, LeaderboardDto>
{
    /// <summary>How many top rows the leaderboard shows before pinning the viewer's own row.</summary>
    public const int TopSize = 50;

    private readonly ILeaderboardStore _leaderboard;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IUserAccountStore _accounts;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly TimeProvider _clock;

    public GetLeaderboardQueryHandler(
        ILeaderboardStore leaderboard,
        ILearnerProfileRepository profiles,
        IUserAccountStore accounts,
        ISubscriptionRepository subscriptions,
        TimeProvider clock)
    {
        _leaderboard = leaderboard;
        _profiles = profiles;
        _accounts = accounts;
        _subscriptions = subscriptions;
        _clock = clock;
    }

    public async Task<LeaderboardDto> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var level = request.Level
            ?? (await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken))?.OverallLevel
            ?? CefrLevel.A2;

        var top = await _leaderboard.GetTopAsync(level, TopSize, cancellationToken);
        var learnerRank = await _leaderboard.GetLearnerRankAsync(request.LearnerId, level, cancellationToken);

        var idsToResolve = top.Select(e => e.LearnerId).ToHashSet();
        if (learnerRank is not null)
            idsToResolve.Add(request.LearnerId);

        var accounts = (await _accounts.GetManyByIdsAsync(idsToResolve, cancellationToken))
            .ToDictionary(a => a.Id);
        var subscriptions = await _subscriptions.GetManyByLearnerIdsAsync(idsToResolve, cancellationToken);
        var now = _clock.GetUtcNow();

        LeaderboardEntryDto ToDto(LeaderboardRankEntry entry)
        {
            accounts.TryGetValue(entry.LearnerId, out var account);
            var isPremium = account?.IsProTrialActive(now) == true
                || subscriptions.TryGetValue(entry.LearnerId, out var subscription)
                && subscription.IsPremiumActive(now);

            return new LeaderboardEntryDto(
                entry.Rank,
                entry.LearnerId,
                account?.DisplayName ?? "?",
                account?.PictureUrl,
                entry.Score,
                isPremium,
                entry.LearnerId == request.LearnerId);
        }

        var topDtos = top.Select(ToDto).ToList();

        // Only pin a separate row when the learner isn't already shown in the top list.
        var currentUserEntry = learnerRank is { Rank: > TopSize } ? ToDto(learnerRank) : null;

        return new LeaderboardDto(level, topDtos, currentUserEntry);
    }
}
