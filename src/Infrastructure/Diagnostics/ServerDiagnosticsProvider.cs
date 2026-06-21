using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Application.Admin.Dtos;
using Application.Admin.Ports;
using Infrastructure.Gamification;
using Infrastructure.Llm;
using Infrastructure.Notifications.Fcm;
using Infrastructure.Persistence;
using Infrastructure.Speaking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Infrastructure.Redis;

namespace Infrastructure.Diagnostics;

/// <summary>
/// Gathers the live server snapshot for the super-admin operations page. Reads the current process
/// (uptime, memory, GC, threads), probes the backing stores it can reach (Postgres via
/// <c>CanConnect</c>, Redis via <c>PING</c>) and reports which external integrations are configured -
/// presence only, never the secret (docs/development-guide.md rule 13). Everything is best-effort: a probe that
/// throws is reported as Unhealthy with its message, never allowed to fail the whole snapshot. Scoped,
/// so it can resolve the request's <see cref="EnglishAiDbContext"/>; Redis and the DB context are
/// resolved optionally (either may be absent in a given deployment).
/// </summary>
public sealed class ServerDiagnosticsProvider : IServerDiagnosticsProvider
{
    private const long BytesPerMb = 1024 * 1024;

    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly IRecentLogStore _logStore;
    private readonly TimeProvider _clock;

    public ServerDiagnosticsProvider(
        IServiceProvider services,
        IConfiguration configuration,
        IRecentLogStore logStore,
        TimeProvider clock)
    {
        _services = services;
        _configuration = configuration;
        _logStore = logStore;
        _clock = clock;
    }

    public async Task<ServerDiagnosticsDto> CollectAsync(CancellationToken cancellationToken)
    {
        var runtime = BuildRuntime();
        var dependencies = new List<DependencyHealthDto>
        {
            await ProbePostgresAsync(cancellationToken),
            await ProbeRedisAsync(cancellationToken),
        };
        var services = BuildServices();
        var logs = BuildLogs();

        return new ServerDiagnosticsDto(_clock.GetUtcNow(), runtime, dependencies, services, logs);
    }

    private ServerRuntimeDto BuildRuntime()
    {
        var process = Process.GetCurrentProcess();
        var startedAt = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
        var uptime = _clock.GetUtcNow() - startedAt;

        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? "unknown";

        return new ServerRuntimeDto(
            Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Version: version,
            Framework: RuntimeInformation.FrameworkDescription,
            OperatingSystem: RuntimeInformation.OSDescription,
            MachineName: Environment.MachineName,
            StartedAtUtc: startedAt,
            UptimeSeconds: (long)Math.Max(0, uptime.TotalSeconds),
            ProcessorCount: Environment.ProcessorCount,
            ManagedMemoryMb: Math.Round((double)GC.GetTotalMemory(forceFullCollection: false) / BytesPerMb, 1),
            WorkingSetMb: Math.Round((double)process.WorkingSet64 / BytesPerMb, 1),
            ThreadCount: process.Threads.Count,
            Gen0Collections: GC.CollectionCount(0),
            Gen1Collections: GC.CollectionCount(1),
            Gen2Collections: GC.CollectionCount(2));
    }

    private async Task<DependencyHealthDto> ProbePostgresAsync(CancellationToken cancellationToken)
    {
        var db = _services.GetService<EnglishAiDbContext>();
        if (db is null)
            return new DependencyHealthDto(
                "PostgreSQL", DependencyStatus.NotConfigured,
                "No connection string configured - running on in-memory stores.", null);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var reachable = await db.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();
            return reachable
                ? new DependencyHealthDto(
                    "PostgreSQL", DependencyStatus.Healthy, "Connection succeeded.", stopwatch.ElapsedMilliseconds)
                : new DependencyHealthDto(
                    "PostgreSQL", DependencyStatus.Unhealthy,
                    "The database is configured but did not accept a connection.", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new DependencyHealthDto(
                "PostgreSQL", DependencyStatus.Unhealthy, Summarize(ex), stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<DependencyHealthDto> ProbeRedisAsync(CancellationToken cancellationToken)
    {
        var redis = _services.GetService<IRedisConnectionProvider>();
        if (redis is null)
            return new DependencyHealthDto(
                "Redis", DependencyStatus.NotConfigured,
                "No connection string configured - gamification/usage use in-memory stores.", null);

        try
        {
            var critical = redis.Get(RedisWorkload.Critical);
            var cache = redis.Get(RedisWorkload.Cache);
            var criticalLatency = await critical.GetDatabase().PingAsync();
            var cacheLatency = await cache.GetDatabase().PingAsync();
            var latency = criticalLatency > cacheLatency ? criticalLatency : cacheLatency;
            return critical.IsConnected && cache.IsConnected
                ? new DependencyHealthDto(
                    "Redis", DependencyStatus.Healthy, "Critical and cache PING succeeded.", (long)latency.TotalMilliseconds)
                : new DependencyHealthDto(
                    "Redis", DependencyStatus.Unhealthy,
                    "Configured but the multiplexer is not connected.", (long)latency.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            return new DependencyHealthDto("Redis", DependencyStatus.Unhealthy, Summarize(ex), null);
        }
    }

    private IReadOnlyList<ExternalServiceDto> BuildServices()
    {
        var azure = _configuration.GetSection(AzureSpeechOptions.SectionName).Get<AzureSpeechOptions>() ?? new AzureSpeechOptions();
        var hermesGateway = _configuration.GetSection(HermesGatewayOptions.SectionName).Get<HermesGatewayOptions>()
                            ?? new HermesGatewayOptions();
        var fcm = _configuration.GetSection(FcmOptions.SectionName).Get<FcmOptions>() ?? new FcmOptions();

        return new List<ExternalServiceDto>
        {
            // The gateway is the only LLM backend (2026-07-28); the third-party providers that used to
            // be listed here alongside it were removed from the product, not just from this panel.
            new("Hermes Agent Gateway", hermesGateway.IsConfigured,
                hermesGateway.IsConfigured
                    ? $"Model: {hermesGateway.Model}"
                    : "No API key - offline fallbacks in use."),
            new("Azure Speech", azure.IsConfigured,
                azure.IsConfigured ? "STT/TTS/pronunciation enabled." : "No key - offline speech estimates in use."),
            new("Push (FCM)", fcm.IsConfigured,
                fcm.IsConfigured ? "Native push enabled." : "No service account - push notifications disabled."),
        };
    }

    private ServerLogSummaryDto BuildLogs()
    {
        var recent = _logStore.Snapshot();
        var warnings = recent.Count(e => string.Equals(e.Level, "Warning", StringComparison.OrdinalIgnoreCase));
        var errors = recent.Count(e =>
            string.Equals(e.Level, "Error", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(e.Level, "Fatal", StringComparison.OrdinalIgnoreCase));

        return new ServerLogSummaryDto(warnings, errors, _logStore.Capacity, recent);
    }

    private static string Summarize(Exception ex)
    {
        var message = ex.GetBaseException().Message;
        return message.Length > 300 ? message[..300] + "…" : message;
    }
}
