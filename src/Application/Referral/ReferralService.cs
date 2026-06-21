using Application.Referral.Ports;
using Domain.Referral;

namespace Application.Referral;

/// <inheritdoc />
public sealed class ReferralService : IReferralService
{
    // A handful of attempts is plenty: the code space (32^6 ≈ 1e9) makes a collision, let alone
    // several in a row, astronomically unlikely.
    private const int MaxCodeGenerationAttempts = 8;

    private readonly IReferralStore _store;
    private readonly TimeProvider _clock;

    public ReferralService(IReferralStore store, TimeProvider clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<ReferralAccount> GetOrCreateAccountAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        var existing = await _store.GetAccountByLearnerAsync(learnerId, cancellationToken);
        if (existing is not null)
            return existing;

        var now = _clock.GetUtcNow();
        for (var attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            var code = ReferralCode.Generate();
            if (await _store.GetAccountByCodeAsync(code, cancellationToken) is not null)
                continue;

            var account = ReferralAccount.Create(learnerId, code, now);
            await _store.SaveAccountAsync(account, cancellationToken);
            return account;
        }

        throw new InvalidOperationException("Could not generate a unique referral code after several attempts.");
    }

    public async Task CaptureAsync(
        Guid refereeId, string? code, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return;

        var normalized = ReferralCode.Normalize(code);
        if (!ReferralCode.IsValid(normalized))
            return;

        // Already referred once? Never overwrite - one referral per referee.
        if (await _store.GetReferralByRefereeAsync(refereeId, cancellationToken) is not null)
            return;

        var referrerAccount = await _store.GetAccountByCodeAsync(normalized, cancellationToken);
        if (referrerAccount is null)
            return; // unknown code - ignore silently so a typo never blocks sign-up

        if (referrerAccount.LearnerId == refereeId)
            return; // self-referral

        var referral = Domain.Referral.Referral.Create(referrerAccount.LearnerId, refereeId, normalized, now);

        // Reward both sides immediately on sign-up: the referee gets +2 topics (+ the small
        // Speaking/Writing bundle) the moment they register, and the referrer's bonus is granted
        // in the same step so it shows up in Profile without waiting for a first lesson. Each
        // account's own lifetime cap still bounds the total payout, so self-referral farming
        // across throwaway accounts stays economically pointless.
        referral.MarkQualified(now);
        await _store.SaveReferralAsync(referral, cancellationToken);

        await GrantRewardAsync(referral.ReferrerId, refereeId, now, cancellationToken);
    }

    public async Task TryQualifyAsync(Guid refereeId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var referral = await _store.GetReferralByRefereeAsync(refereeId, cancellationToken);
        if (referral is null || referral.Status != ReferralStatus.Pending)
            return; // rewards are now granted at capture; an already-qualified referral is a no-op

        if (!referral.MarkQualified(now))
            return;

        await _store.SaveReferralAsync(referral, cancellationToken);
        await GrantRewardAsync(referral.ReferrerId, refereeId, now, cancellationToken);
    }

    /// <summary>
    /// Pays the capped reward bundle to both the referrer and the referee exactly once. Each
    /// side's <see cref="ReferralAccount"/> enforces its own lifetime cap, so this stays honest
    /// and idempotent regardless of when it is called.
    /// </summary>
    private async Task GrantRewardAsync(
        Guid referrerId, Guid refereeId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var referrerAccount = await GetOrCreateAccountAsync(referrerId, cancellationToken);
        if (referrerAccount.GrantReferralReward(now))
            await _store.SaveAccountAsync(referrerAccount, cancellationToken);

        var refereeAccount = await GetOrCreateAccountAsync(refereeId, cancellationToken);
        if (refereeAccount.GrantReferralReward(now))
            await _store.SaveAccountAsync(refereeAccount, cancellationToken);
    }
}
