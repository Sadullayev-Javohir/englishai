namespace Application.Admin.Dtos;

/// <summary>
/// A live snapshot of how the server is running, for the super-admin operations page
/// (<c>/admin/server</c>). Assembled by <see cref="Application.Admin.Ports.IServerDiagnosticsProvider"/>
/// from the running process (runtime metrics), the backing stores (Postgres/Redis reachability), the
/// configured external services (Gemini/Azure/Claude/FCM - presence only, never the secret itself,
/// per docs/development-guide.md rule 13) and a rolling buffer of recent warnings/errors. Purely a read model: it
/// changes nothing, so it is safe to poll.
/// </summary>
public sealed record ServerDiagnosticsDto(
    DateTimeOffset GeneratedAtUtc,
    ServerRuntimeDto Runtime,
    IReadOnlyList<DependencyHealthDto> Dependencies,
    IReadOnlyList<ExternalServiceDto> Services,
    ServerLogSummaryDto Logs)
{
    /// <summary>
    /// The worst dependency status, so the UI can render one overall banner: "Unhealthy" if any
    /// configured dependency is down, otherwise "Degraded" if any is not configured, otherwise
    /// "Healthy".
    /// </summary>
    public string OverallStatus =>
        Dependencies.Any(d => d.Status == DependencyStatus.Unhealthy) ? DependencyStatus.Unhealthy
        : Dependencies.Any(d => d.Status == DependencyStatus.NotConfigured) ? DependencyStatus.Degraded
        : DependencyStatus.Healthy;
}

/// <summary>A bounded time-series point used by the live server monitoring charts.</summary>
public sealed record ServerTelemetryPointDto(
    DateTimeOffset TimestampUtc,
    double ManagedMemoryMb,
    double WorkingSetMb,
    int ThreadCount,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    int WarningCount,
    int ErrorCount,
    IReadOnlyDictionary<string, long?> DependencyLatenciesMs);

/// <summary>Initial state sent to a server monitoring stream subscriber.</summary>
public sealed record ServerTelemetryHistoryDto(
    ServerDiagnosticsDto Current,
    IReadOnlyList<ServerTelemetryPointDto> History,
    int SampleIntervalSeconds,
    int RetentionMinutes);

/// <summary>Process / runtime vitals of the running server.</summary>
public sealed record ServerRuntimeDto(
    string Environment,
    string Version,
    string Framework,
    string OperatingSystem,
    string MachineName,
    DateTimeOffset StartedAtUtc,
    long UptimeSeconds,
    int ProcessorCount,
    double ManagedMemoryMb,
    double WorkingSetMb,
    int ThreadCount,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections);

/// <summary>
/// Reachability of one backing store the app depends on (Postgres, Redis, …). <see cref="Status"/>
/// is one of the <see cref="DependencyStatus"/> constants; <see cref="LatencyMs"/> is the probe
/// round-trip when it succeeded.
/// </summary>
public sealed record DependencyHealthDto(
    string Name,
    string Status,
    string Detail,
    long? LatencyMs);

/// <summary>String constants for <see cref="DependencyHealthDto.Status"/> (serialized as-is).</summary>
public static class DependencyStatus
{
    public const string Healthy = "Healthy";
    public const string Unhealthy = "Unhealthy";
    public const string NotConfigured = "NotConfigured";
    public const string Degraded = "Degraded";
}

/// <summary>
/// Whether an external service is wired up. <see cref="Configured"/> reports only presence of a
/// credential/config - never the secret - so an operator can see at a glance which integrations are
/// live on this deployment.
/// </summary>
public sealed record ExternalServiceDto(
    string Name,
    bool Configured,
    string Detail);

/// <summary>Recent-log rollup: how many warnings/errors are in the buffer, plus the entries.</summary>
public sealed record ServerLogSummaryDto(
    int WarningCount,
    int ErrorCount,
    int Capacity,
    IReadOnlyList<LogEntryDto> Recent);

/// <summary>
/// One captured log line (newest first in <see cref="ServerLogSummaryDto.Recent"/>). The
/// <see cref="Id"/> is a per-entry handle the super-admin server page uses to dismiss a single line
/// from the in-memory buffer; the log pipeline assigns a fresh one, so it defaults to empty for the
/// rare direct construction (e.g. tests).
/// </summary>
public sealed record LogEntryDto(
    DateTimeOffset TimestampUtc,
    string Level,
    string Message,
    string? Exception,
    Guid Id = default,
    string? SourceContext = null,
    string? RequestPath = null,
    string? CorrelationId = null,
    string? TraceId = null);

public sealed record OperationsDiagnosticsDto(
    DateTimeOffset GeneratedAtUtc,
    string OverallStatus,
    IReadOnlyList<DependencyHealthDto> Dependencies,
    int WarningCount,
    int ErrorCount,
    IReadOnlyList<OperationsLogEntryDto> Recent);

public sealed record OperationsLogEntryDto(
    Guid Id,
    DateTimeOffset TimestampUtc,
    string Level,
    string Message);
