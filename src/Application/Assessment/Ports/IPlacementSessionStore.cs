using Domain.Assessment;

namespace Application.Assessment.Ports;

/// <summary>
/// Persistence for in-flight placement test sessions. In-memory for Faza 0;
/// backed by a durable store (PostgreSQL/Redis) in a later phase.
/// </summary>
public interface IPlacementSessionStore
{
    Task<PlacementTestSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task SaveAsync(PlacementTestSession session, CancellationToken cancellationToken = default);
}
