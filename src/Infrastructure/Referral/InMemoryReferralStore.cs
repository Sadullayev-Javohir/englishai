using System.Collections.Concurrent;
using Application.Referral.Ports;
using Domain.Referral;

namespace Infrastructure.Referral;

/// <summary>
/// Dev/test <see cref="IReferralStore"/> - process-local maps of referral accounts (by learner)
/// and referrals (by referee). Used when no database is configured; EF Core is the durable path.
/// Registered as a singleton so data persists across requests within a process run.
/// </summary>
public sealed class InMemoryReferralStore : IReferralStore
{
    private readonly ConcurrentDictionary<Guid, ReferralAccount> _accountsByLearner = new();
    private readonly ConcurrentDictionary<Guid, Domain.Referral.Referral> _referralsByReferee = new();

    public Task<ReferralAccount?> GetAccountByLearnerAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        _accountsByLearner.TryGetValue(learnerId, out var account);
        return Task.FromResult(account);
    }

    public Task<ReferralAccount?> GetAccountByCodeAsync(string normalizedCode, CancellationToken cancellationToken) =>
        Task.FromResult(_accountsByLearner.Values.FirstOrDefault(a => a.Code == normalizedCode));

    public Task SaveAccountAsync(ReferralAccount account, CancellationToken cancellationToken)
    {
        _accountsByLearner[account.LearnerId] = account;
        return Task.CompletedTask;
    }

    public Task<Domain.Referral.Referral?> GetReferralByRefereeAsync(
        Guid refereeId, CancellationToken cancellationToken)
    {
        _referralsByReferee.TryGetValue(refereeId, out var referral);
        return Task.FromResult(referral);
    }

    public Task<IReadOnlyList<Domain.Referral.Referral>> GetReferralsByReferrerAsync(
        Guid referrerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Referral.Referral> sent = _referralsByReferee.Values
            .Where(r => r.ReferrerId == referrerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        return Task.FromResult(sent);
    }

    public Task SaveReferralAsync(Domain.Referral.Referral referral, CancellationToken cancellationToken)
    {
        _referralsByReferee[referral.RefereeId] = referral;
        return Task.CompletedTask;
    }
}
