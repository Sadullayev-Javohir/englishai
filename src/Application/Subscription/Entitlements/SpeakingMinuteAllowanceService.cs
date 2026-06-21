using Application.Common;
using Application.Subscription.Access;
using Application.Subscription.Ports;
using Domain.Subscription;

namespace Application.Subscription.Entitlements;

public sealed class SpeakingMinuteAllowanceService : ISpeakingMinuteAllowanceService
{
    private readonly IProAccessService _proAccess;
    private readonly IMeteredAllowanceStore _allowances;
    private readonly TimeProvider _clock;

    public SpeakingMinuteAllowanceService(
        IProAccessService proAccess,
        IMeteredAllowanceStore allowances,
        TimeProvider clock)
    {
        _proAccess = proAccess;
        _allowances = allowances;
        _clock = clock;
    }

    public async Task<SpeakingMinuteDecision> EvaluateAsync(
        Guid learnerId, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow();
        var isPremium = (await _proAccess.EvaluateAsync(learnerId, cancellationToken)).HasFullAccess;
        var used = await _allowances.GetAsync(
            MeteredAllowanceScope.SpeakingMinutes, Subject(learnerId), UsagePeriodKey.Daily(now), cancellationToken);
        return SpeakingMinuteAllowance.Evaluate(isPremium, used);
    }

    public async Task EnsureAllowedAsync(Guid learnerId, CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateAsync(learnerId, cancellationToken);
        if (decision.IsAllowed)
            return;

        throw new SpeakingMinutesExhaustedException(
            decision.LimitMinutes, decision.UsedMinutes, NextLocalMidnight(_clock.GetUtcNow()));
    }

    public async Task<SpeakingMinuteDecision> RecordAsync(
        Guid learnerId, double minutes, CancellationToken cancellationToken = default)
    {
        var now = _clock.GetUtcNow();
        var isPremium = (await _proAccess.EvaluateAsync(learnerId, cancellationToken)).HasFullAccess;

        // A non-finite or negative duration means the audio could not be measured; booking it would
        // either poison the counter or hand out free allowance. Read the current total instead.
        var billable = double.IsFinite(minutes) && minutes > 0 ? minutes : 0;
        var periodKey = UsagePeriodKey.Daily(now);
        var used = billable > 0
            ? await _allowances.AddAsync(
                MeteredAllowanceScope.SpeakingMinutes, Subject(learnerId), periodKey, billable, cancellationToken)
            : await _allowances.GetAsync(
                MeteredAllowanceScope.SpeakingMinutes, Subject(learnerId), periodKey, cancellationToken);

        return SpeakingMinuteAllowance.Evaluate(isPremium, used);
    }

    private static string Subject(Guid learnerId) => learnerId.ToString("N");

    /// <summary>The allowance renews with the learner's local day (UTC+5), matching every other quota.</summary>
    private static DateTimeOffset NextLocalMidnight(DateTimeOffset now)
    {
        var local = now.ToOffset(AppClock.UzbekistanOffset);
        return new DateTimeOffset(local.Date.AddDays(1), AppClock.UzbekistanOffset);
    }
}
