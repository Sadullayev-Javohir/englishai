using System.Collections.Concurrent;
using Application.Speaking.Models;
using Application.Speaking.Ports;

namespace Infrastructure.Speaking;

/// <summary>
/// Process-wide in-memory cache of per-word synthesized speech (reference audio + red-lips
/// viseme track). The vocabulary is bounded, but the cache is capped and evicts an arbitrary
/// entry when full so it can never grow without limit. Suitable for a single instance; a
/// distributed cache (Redis) can replace it behind <see cref="IWordVisemeCache"/> when the
/// app scales out.
/// </summary>
public sealed class InMemoryWordVisemeCache : IWordVisemeCache
{
    private const int MaxEntries = 2000;

    private readonly ConcurrentDictionary<string, SynthesizedSpeech> _byWord =
        new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string word, out SynthesizedSpeech speech)
    {
        if (_byWord.TryGetValue(word, out var cached))
        {
            speech = cached;
            return true;
        }

        speech = null!;
        return false;
    }

    public void Set(string word, SynthesizedSpeech speech)
    {
        if (_byWord.Count >= MaxEntries && !_byWord.ContainsKey(word))
        {
            // Drop one arbitrary entry to stay under the cap (good enough for a bounded
            // word set; not LRU - a distributed cache would handle eviction properly).
            var victim = _byWord.Keys.FirstOrDefault();
            if (victim is not null)
                _byWord.TryRemove(victim, out _);
        }

        _byWord[word] = speech;
    }
}
