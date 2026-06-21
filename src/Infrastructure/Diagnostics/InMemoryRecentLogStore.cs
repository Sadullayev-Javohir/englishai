using Application.Admin.Dtos;
using Application.Admin.Ports;

namespace Infrastructure.Diagnostics;

/// <summary>
/// A fixed-size, thread-safe ring buffer of recent warning/error log lines (<see cref="IRecentLogStore"/>).
/// The Web layer's Serilog sink pushes into it; the server-diagnostics page reads it. It holds at most
/// <see cref="Capacity"/> entries in memory (never touches disk or a network), so it adds no cost and
/// cannot grow unbounded - the oldest line is dropped when full. Registered as a singleton so it lives
/// for the whole process.
/// </summary>
public sealed class InMemoryRecentLogStore : IRecentLogStore
{
    private readonly int _capacity;
    private readonly LinkedList<LogEntryDto> _entries = new();
    private readonly object _gate = new();

    public InMemoryRecentLogStore(int capacity = 100)
    {
        _capacity = capacity < 1 ? 1 : capacity;
    }

    public int Capacity => _capacity;

    public void Add(LogEntryDto entry)
    {
        if (entry is null)
            return;

        lock (_gate)
        {
            _entries.AddFirst(entry);
            while (_entries.Count > _capacity)
                _entries.RemoveLast();
        }
    }

    public IReadOnlyList<LogEntryDto> Snapshot()
    {
        lock (_gate)
        {
            // Already newest-first (AddFirst); copy out so callers never see the live list.
            return _entries.ToArray();
        }
    }

    public bool Remove(Guid id)
    {
        if (id == Guid.Empty)
            return false;

        lock (_gate)
        {
            var node = _entries.First;
            while (node is not null)
            {
                if (node.Value.Id == id)
                {
                    _entries.Remove(node);
                    return true;
                }
                node = node.Next;
            }
            return false;
        }
    }

    public int Clear()
    {
        lock (_gate)
        {
            var count = _entries.Count;
            _entries.Clear();
            return count;
        }
    }
}
