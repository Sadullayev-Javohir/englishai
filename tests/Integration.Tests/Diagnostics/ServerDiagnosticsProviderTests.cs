using Application.Admin.Dtos;
using FluentAssertions;
using Infrastructure.Diagnostics;
using Integration.Tests.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Diagnostics;

/// <summary>
/// Exercises the real <see cref="ServerDiagnosticsProvider"/> against an empty service graph (no
/// Postgres, no Redis). It must still produce a full snapshot - runtime vitals from the live process,
/// both dependencies reported as NotConfigured (never throwing), and each external service reported
/// as unconfigured - so the operations page renders even on a bare deployment.
/// </summary>
public class ServerDiagnosticsProviderTests
{
    [Fact]
    public async Task Collects_a_snapshot_with_no_database_or_redis_configured()
    {
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        var configuration = new ConfigurationBuilder().Build();
        var logStore = new InMemoryRecentLogStore(capacity: 50);
        logStore.Add(new LogEntryDto(DateTimeOffset.UtcNow, "Error", "boom", "stack"));
        logStore.Add(new LogEntryDto(DateTimeOffset.UtcNow, "Warning", "careful", null));

        using var services = new ServiceCollection().BuildServiceProvider();
        var provider = new ServerDiagnosticsProvider(services, configuration, logStore, TimeProvider.System);

        var snapshot = await provider.CollectAsync(CancellationToken.None);

        // Runtime is read from the live process.
        snapshot.Runtime.ProcessorCount.Should().BeGreaterThan(0);
        snapshot.Runtime.Framework.Should().NotBeNullOrWhiteSpace();

        // Both backing stores are absent → NotConfigured, and nothing threw.
        snapshot.Dependencies.Should().Contain(d => d.Name == "PostgreSQL" && d.Status == DependencyStatus.NotConfigured);
        snapshot.Dependencies.Should().Contain(d => d.Name == "Redis" && d.Status == DependencyStatus.NotConfigured);

        // With no config, every external integration is reported as unconfigured.
        snapshot.Services.Should().OnlyContain(s => s.Configured == false);
        snapshot.Services.Select(s => s.Name).Should().Contain("Hermes Agent Gateway");

        // The log buffer is surfaced and counted by level.
        snapshot.Logs.ErrorCount.Should().Be(1);
        snapshot.Logs.WarningCount.Should().Be(1);
        snapshot.Logs.Recent.Should().HaveCount(2);
        snapshot.OverallStatus.Should().Be(DependencyStatus.Degraded); // NotConfigured, none Unhealthy
    }
}
