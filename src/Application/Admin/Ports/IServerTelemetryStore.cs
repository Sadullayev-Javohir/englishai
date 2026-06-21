using Application.Admin.Dtos;

namespace Application.Admin.Ports;

public interface IServerTelemetryStore
{
    int SampleIntervalSeconds { get; }
    int RetentionMinutes { get; }
    ServerDiagnosticsDto? Current { get; }
    void Append(ServerDiagnosticsDto snapshot);
    IReadOnlyList<ServerTelemetryPointDto> History();
    IAsyncEnumerable<ServerDiagnosticsDto> SubscribeAsync(CancellationToken cancellationToken);
}
