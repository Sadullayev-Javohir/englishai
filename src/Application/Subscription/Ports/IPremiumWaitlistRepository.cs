using Domain.Subscription;

namespace Application.Subscription.Ports;

public interface IPremiumWaitlistRepository
{
    /// <summary>The normalized contact, so a repeat submission updates rather than duplicating.</summary>
    Task<PremiumWaitlistEntry?> GetByContactAsync(string contact, CancellationToken cancellationToken);

    Task AddAsync(PremiumWaitlistEntry entry, CancellationToken cancellationToken);

    /// <summary>Newest first, for the admin view.</summary>
    Task<IReadOnlyList<PremiumWaitlistEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);
}
