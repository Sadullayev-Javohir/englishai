using Application.Referral.Ports;
using Domain.Referral;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Referral;

/// <summary>EF Core (PostgreSQL) <see cref="IReferralStore"/> - durable referral accounts and links.</summary>
public sealed class EfReferralStore : IReferralStore
{
    private readonly EnglishAiDbContext _db;

    public EfReferralStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public Task<ReferralAccount?> GetAccountByLearnerAsync(Guid learnerId, CancellationToken cancellationToken) =>
        _db.ReferralAccounts.FirstOrDefaultAsync(a => a.LearnerId == learnerId, cancellationToken);

    public Task<ReferralAccount?> GetAccountByCodeAsync(string normalizedCode, CancellationToken cancellationToken) =>
        _db.ReferralAccounts.FirstOrDefaultAsync(a => a.Code == normalizedCode, cancellationToken);

    public async Task SaveAccountAsync(ReferralAccount account, CancellationToken cancellationToken)
    {
        var exists = await _db.ReferralAccounts.AnyAsync(a => a.Id == account.Id, cancellationToken);
        if (exists)
            _db.ReferralAccounts.Update(account);
        else
            await _db.ReferralAccounts.AddAsync(account, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<Domain.Referral.Referral?> GetReferralByRefereeAsync(
        Guid refereeId, CancellationToken cancellationToken) =>
        _db.Referrals.FirstOrDefaultAsync(r => r.RefereeId == refereeId, cancellationToken);

    public async Task<IReadOnlyList<Domain.Referral.Referral>> GetReferralsByReferrerAsync(
        Guid referrerId, CancellationToken cancellationToken) =>
        await _db.Referrals
            .AsNoTracking()
            .Where(r => r.ReferrerId == referrerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task SaveReferralAsync(Domain.Referral.Referral referral, CancellationToken cancellationToken)
    {
        var exists = await _db.Referrals.AnyAsync(r => r.Id == referral.Id, cancellationToken);
        if (exists)
            _db.Referrals.Update(referral);
        else
            await _db.Referrals.AddAsync(referral, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
