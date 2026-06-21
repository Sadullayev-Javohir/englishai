using Application.Admin.Dtos;

namespace Application.Admin.Ports;

/// <summary>
/// Collects a live <see cref="ServerDiagnosticsDto"/> for the super-admin operations page. Implemented
/// in Infrastructure (it reads the process, pings the backing stores and inspects configuration); the
/// Application handler only authorizes the caller and delegates here, so the layering stays intact.
/// </summary>
public interface IServerDiagnosticsProvider
{
    Task<ServerDiagnosticsDto> CollectAsync(CancellationToken cancellationToken);
}
