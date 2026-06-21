using Application.Common;
using Application.Subscription.Access;
using Application.Subscription.Entitlements;
using MediatR;

namespace Application.Speaking.GetSpeakingQuota;

public sealed class GetSpeakingQuotaQueryHandler
    : IRequestHandler<GetSpeakingQuotaQuery, SpeakingQuotaStatusDto>
{
    private readonly ISpeakingMinuteAllowanceService _allowance;
    private readonly IProAccessService _proAccess;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;

    public GetSpeakingQuotaQueryHandler(
        ISpeakingMinuteAllowanceService allowance,
        IProAccessService proAccess,
        TimeProvider clock,
        ICurrentUserAccessor? currentUser = null)
    {
        _allowance = allowance;
        _proAccess = proAccess;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<SpeakingQuotaStatusDto> Handle(
        GetSpeakingQuotaQuery request, CancellationToken cancellationToken)
    {
        ResourceOwnership.EnsureCurrentLearner(_currentUser, request.LearnerId);

        var decision = await _allowance.EvaluateAsync(request.LearnerId, cancellationToken);
        var access = await _proAccess.EvaluateAsync(request.LearnerId, cancellationToken);
        var local = _clock.GetUtcNow().ToOffset(AppClock.UzbekistanOffset);

        return new SpeakingQuotaStatusDto(
            Math.Round(decision.LimitMinutes, 2),
            Math.Round(decision.UsedMinutes, 2),
            Math.Round(decision.RemainingMinutes, 2),
            decision.IsAllowed,
            access.HasFullAccess,
            new DateTimeOffset(local.Date.AddDays(1), AppClock.UzbekistanOffset));
    }
}
