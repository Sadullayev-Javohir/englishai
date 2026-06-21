using System.Collections.Concurrent;
using Application.Translation.Ports;

namespace Infrastructure.Translation;

/// <summary>
/// Process-wide in-memory cache of English→Uzbek sentence translations. Teaching sentences are a
/// bounded, heavily repeating set, but the cache is capped and evicts an arbitrary entry when full
/// so it can never grow without limit. Suitable for a single instance; a distributed cache (Redis)
/// can replace it behind <see cref="ITranslationCache"/> when the app scales out.
/// </summary>
public sealed class InMemoryTranslationCache : ITranslationCache
{
    private const int MaxEntries = 10000;

    private readonly ConcurrentDictionary<string, string> _byKey = new(StringComparer.Ordinal);

    public bool TryGet(string key, out string translation)
    {
        if (_byKey.TryGetValue(key, out var cached))
        {
            translation = cached;
            return true;
        }

        translation = string.Empty;
        return false;
    }

    public void Set(string key, string translation)
    {
        if (_byKey.Count >= MaxEntries && !_byKey.ContainsKey(key))
        {
            // Drop one arbitrary entry to stay under the cap (good enough for a bounded sentence
            // set; not LRU - a distributed cache would handle eviction properly).
            var victim = _byKey.Keys.FirstOrDefault();
            if (victim is not null)
                _byKey.TryRemove(victim, out _);
        }

        _byKey[key] = translation;
    }
}
