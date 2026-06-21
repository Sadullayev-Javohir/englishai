using Application.Learning.Ports;
using Domain.Learning;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Learning;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="ILearnerProfileRepository"/>. Owned
/// collections (seeds, activities, errors) load and persist with the aggregate.
/// </summary>
public sealed class EfLearnerProfileRepository : ILearnerProfileRepository
{
    private readonly EnglishAiDbContext _db;

    public EfLearnerProfileRepository(EnglishAiDbContext db)
    {
        _db = db;
    }

    // LearnerProfile owns three collections (Seeds, Activities, Errors) that all load with the
    // aggregate. Loading them in one SQL statement multiplies the rows (a cartesian join) and makes
    // EF warn about it (MultipleCollectionIncludeWarning). AsSplitQuery loads each collection in its
    // own round trip instead - no row explosion, and the warning is gone. A by-id aggregate read
    // like this has no pagination, so split-query result consistency is not a concern here.
    public Task<LearnerProfile?> GetByLearnerIdAsync(Guid learnerId, CancellationToken cancellationToken) =>
        _db.LearnerProfiles.AsSplitQuery().FirstOrDefaultAsync(p => p.LearnerId == learnerId, cancellationToken);

    public async Task<IReadOnlyList<LearnerProfile>> GetAllAsync(CancellationToken cancellationToken) =>
        await _db.LearnerProfiles.AsSplitQuery().ToListAsync(cancellationToken);

    public async Task SaveAsync(LearnerProfile profile, CancellationToken cancellationToken)
    {
        await TrackAsync(profile, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackAsync(LearnerProfile profile, CancellationToken cancellationToken)
    {
        // A freshly created profile is detached; a loaded one is already tracked.
        if (_db.Entry(profile).State == EntityState.Detached)
            await _db.LearnerProfiles.AddAsync(profile, cancellationToken);
    }
}
