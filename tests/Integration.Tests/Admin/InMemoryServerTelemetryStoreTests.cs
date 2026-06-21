using Application.Admin.Dtos;
using FluentAssertions;
using Infrastructure.Diagnostics;
using Xunit;

namespace Integration.Tests.Admin;

public class InMemoryServerTelemetryStoreTests
{
    [Fact]
    public void Append_keeps_a_bounded_ordered_history_and_ignores_duplicates()
    {
        var store = new InMemoryServerTelemetryStore(sampleIntervalSeconds: 30, retentionMinutes: 1);
        var start = DateTimeOffset.Parse("2026-08-08T00:00:00Z");

        store.Append(Snapshot(start, 10));
        store.Append(Snapshot(start, 99));
        store.Append(Snapshot(start.AddSeconds(30), 20));
        store.Append(Snapshot(start.AddSeconds(60), 30));

        store.History().Select(point => point.ManagedMemoryMb).Should().Equal(20, 30);
        store.Current!.Runtime.ManagedMemoryMb.Should().Be(30);
    }

    private static ServerDiagnosticsDto Snapshot(DateTimeOffset at, double memory) => new(
        at,
        new ServerRuntimeDto("Test", "1", ".NET", "Linux", "test", at, 0, 4, memory, memory * 2, 8, 1, 2, 3),
        [new DependencyHealthDto("PostgreSQL", DependencyStatus.Healthy, "ok", 4)],
        [],
        new ServerLogSummaryDto(2, 1, 100, []));
}
