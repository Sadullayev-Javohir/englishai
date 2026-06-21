using Domain.Developer;

namespace Application.Developer.Ports;

public interface IDeveloperApiKeyStore
{
    Task AddAsync(DeveloperApiKey apiKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeveloperApiKey>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<DeveloperApiKey?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DeveloperApiKey>> GetActiveByPrefixAsync(string prefix, CancellationToken cancellationToken);
    Task UpdateAsync(DeveloperApiKey apiKey, CancellationToken cancellationToken);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
