using Application.Admin.Dtos;

namespace Application.Admin.Ports;

/// <summary>
/// A small, in-process rolling buffer of the most recent warning/error log lines, so the super-admin
/// server page can surface "what's going wrong right now" without shipping a log-aggregation stack.
/// The Web layer feeds it from a Serilog sink; the diagnostics provider reads it. Bounded (oldest
/// entries drop) and thread-safe - it must never grow unbounded or block the logging pipeline.
/// </summary>
public interface IRecentLogStore
{
    /// <summary>The maximum number of entries retained.</summary>
    int Capacity { get; }

    /// <summary>Records one entry, evicting the oldest when at capacity. Called from the log pipeline.</summary>
    void Add(LogEntryDto entry);

    /// <summary>The retained entries, newest first.</summary>
    IReadOnlyList<LogEntryDto> Snapshot();

    /// <summary>Drops the entry with the given id (super-admin dismiss). Returns false if not found.</summary>
    bool Remove(Guid id);

    /// <summary>Empties the buffer (super-admin "clear all"). Returns how many entries were removed.</summary>
    int Clear();
}
