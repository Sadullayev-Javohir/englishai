using System.Collections.Concurrent;

namespace Web.Hubs;

public sealed class SpeakingLiveTurnRegistry
{
    private readonly ConcurrentDictionary<Guid, TurnState> _turns = new();

    public TurnClaim Claim(Guid turnId, int cacheSize)
    {
        var state = _turns.GetOrAdd(turnId, static _ => new TurnState());
        lock (state)
        {
            if (state.Completed)
                return TurnClaim.Completed;
            if (state.Processing)
                return TurnClaim.Processing;

            state.Processing = true;
            Trim(Math.Max(32, cacheSize));
            return TurnClaim.Acquired;
        }
    }

    public void Complete(Guid turnId)
    {
        var state = _turns.GetOrAdd(turnId, static _ => new TurnState());
        lock (state)
        {
            state.Processing = false;
            state.Completed = true;
            state.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Release(Guid turnId)
    {
        if (!_turns.TryGetValue(turnId, out var state))
            return;
        lock (state)
        {
            state.Processing = false;
            state.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private void Trim(int cacheSize)
    {
        if (_turns.Count <= cacheSize)
            return;

        foreach (var item in _turns
                     .Where(item => !item.Value.Processing)
                     .OrderBy(item => item.Value.UpdatedAt)
                     .Take(_turns.Count - cacheSize))
        {
            _turns.TryRemove(item.Key, out _);
        }
    }

    private sealed class TurnState
    {
        public bool Processing { get; set; }
        public bool Completed { get; set; }
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

public enum TurnClaim
{
    Acquired,
    Processing,
    Completed,
}
