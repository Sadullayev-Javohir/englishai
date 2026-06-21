using Application.Identity.Ports;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IAccountEraser"/>. Deletes every durable row
/// keyed by the learner id with set-based <c>ExecuteDeleteAsync</c> calls. Shared catalog and
/// content tables (topics, passages, videos, lessons, books, images) are not learner-scoped and
/// are deliberately left untouched.
/// </summary>
public sealed class EfAccountEraser : IAccountEraser
{
    private readonly EnglishAiDbContext _db;

    public EfAccountEraser(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task EraseAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        await _db.DeveloperApiKeys.Where(k => k.UserId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.UserPreferences.Where(p => p.UserId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.LearnerProfiles.Where(p => p.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.VocabularyItems.Where(v => v.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TopicSpeakingProgress.Where(t => t.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.TopicCompletionRecords.Where(t => t.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.Subscriptions.Where(s => s.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.Payments.Where(p => p.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.BookProgress.Where(b => b.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.DailyStudyRecords.Where(d => d.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.ReferralAccounts.Where(a => a.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.Referrals.Where(r => r.ReferrerId == learnerId || r.RefereeId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.LearnerPoints.Where(p => p.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.EnergySpends.Where(s => s.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
        await _db.DiscountRedemptions.Where(r => r.LearnerId == learnerId)
            .ExecuteDeleteAsync(cancellationToken);

        // The account row last: if an earlier delete fails the account still exists, so the
        // operation can be safely retried rather than leaving an unreachable, half-wiped account.
        await _db.UserAccounts.Where(a => a.Id == learnerId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
