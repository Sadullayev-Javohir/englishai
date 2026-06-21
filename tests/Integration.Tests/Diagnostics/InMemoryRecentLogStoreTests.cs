using Application.Admin.Dtos;
using FluentAssertions;
using Infrastructure.Diagnostics;
using Xunit;

namespace Integration.Tests.Diagnostics;

/// <summary>
/// The recent-log ring buffer must stay bounded (never leak memory), keep newest lines first, and
/// evict the oldest once full - it tails the log stream for the super-admin server page.
/// </summary>
public class InMemoryRecentLogStoreTests
{
    private static LogEntryDto Entry(string message) =>
        new(DateTimeOffset.UnixEpoch, "Error", message, null);

    private static LogEntryDto Entry(string message, Guid id) =>
        new(DateTimeOffset.UnixEpoch, "Error", message, null, id);

    [Fact]
    public void Snapshot_returns_newest_first()
    {
        var store = new InMemoryRecentLogStore(capacity: 10);

        store.Add(Entry("first"));
        store.Add(Entry("second"));

        store.Snapshot().Select(e => e.Message).Should().ContainInOrder("second", "first");
    }

    [Fact]
    public void The_buffer_is_bounded_and_evicts_the_oldest()
    {
        var store = new InMemoryRecentLogStore(capacity: 3);

        for (var i = 1; i <= 5; i++)
            store.Add(Entry($"line-{i}"));

        var snapshot = store.Snapshot();
        snapshot.Should().HaveCount(3);
        // Newest three survive; the first two were evicted.
        snapshot.Select(e => e.Message).Should().ContainInOrder("line-5", "line-4", "line-3");
    }

    [Fact]
    public void Remove_drops_the_matching_entry_and_leaves_the_rest()
    {
        var store = new InMemoryRecentLogStore(capacity: 10);
        var target = Guid.NewGuid();
        store.Add(Entry("keep-1", Guid.NewGuid()));
        store.Add(Entry("drop-me", target));
        store.Add(Entry("keep-2", Guid.NewGuid()));

        store.Remove(target).Should().BeTrue();

        store.Snapshot().Select(e => e.Message).Should().BeEquivalentTo(new[] { "keep-2", "keep-1" });
    }

    [Fact]
    public void Remove_returns_false_for_an_unknown_or_empty_id()
    {
        var store = new InMemoryRecentLogStore(capacity: 10);
        store.Add(Entry("only", Guid.NewGuid()));

        store.Remove(Guid.NewGuid()).Should().BeFalse();
        store.Remove(Guid.Empty).Should().BeFalse();
        store.Snapshot().Should().HaveCount(1);
    }

    [Fact]
    public void Clear_empties_the_buffer_and_reports_the_count()
    {
        var store = new InMemoryRecentLogStore(capacity: 10);
        store.Add(Entry("a"));
        store.Add(Entry("b"));

        store.Clear().Should().Be(2);
        store.Snapshot().Should().BeEmpty();
    }
}
