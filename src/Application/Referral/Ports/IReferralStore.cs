using Domain.Referral;

namespace Application.Referral.Ports;

/// <summary>
/// Persistence port for the referral aggregates: one <see cref="ReferralAccount"/> per learner
/// (their code + earned bonus balances) and one <see cref="Domain.Referral.Referral"/> per
/// referred friend. EF Core adapter in production, in-memory in dev/tests (docs/development-guide.md rule 10).
/// </summary>
public interface IReferralStore
{
    /// <summary>The learner's referral account, or null if one has not been created yet.</summary>
    Task<ReferralAccount?> GetAccountByLearnerAsync(Guid learnerId, CancellationToken cancellationToken);

    /// <summary>The account owning a (normalized) code, or null if the code is unknown.</summary>
    Task<ReferralAccount?> GetAccountByCodeAsync(string normalizedCode, CancellationToken cancellationToken);

    /// <summary>Persists a new or updated referral account.</summary>
    Task SaveAccountAsync(ReferralAccount account, CancellationToken cancellationToken);

    /// <summary>The referral recording how this learner was referred, or null if they were not.</summary>
    Task<Domain.Referral.Referral?> GetReferralByRefereeAsync(Guid refereeId, CancellationToken cancellationToken);

    /// <summary>Every referral this learner has sent out (to compute invited/qualified counts).</summary>
    Task<IReadOnlyList<Domain.Referral.Referral>> GetReferralsByReferrerAsync(
        Guid referrerId, CancellationToken cancellationToken);

    /// <summary>Persists a new or updated referral.</summary>
    Task SaveReferralAsync(Domain.Referral.Referral referral, CancellationToken cancellationToken);
}
