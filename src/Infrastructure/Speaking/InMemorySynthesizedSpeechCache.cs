using System.Collections.Concurrent;
using Application.Speaking.Models;
using Application.Speaking.Ports;

namespace Infrastructure.Speaking;

/// <summary>
/// Process-local synthesis cache, used when Redis is not configured (development, tests) and as the
/// L1 in front of Redis in production - a hot tutor opening then costs neither an Azure call nor a
/// Redis round-trip plus base64 decode. Capped and evicts an arbitrary entry when full, matching
/// <see cref="InMemoryWordVisemeCache"/>; it is a cache, so an unlucky eviction only costs one
/// re-synthesis.
/// </summary>
public sealed class InMemorySynthesizedSpeechCache : ISynthesizedSpeechCache
{
    private const int MaxEntries = 512;

    private readonly ConcurrentDictionary<string, SynthesizedSpeech> _entries = new(StringComparer.Ordinal);

    public Task<SynthesizedSpeech?> GetAsync(
        string voiceName, string text, CancellationToken cancellationToken = default)
    {
        if (!SynthesizedSpeechCacheKey.IsCacheable(text))
            return Task.FromResult<SynthesizedSpeech?>(null);

        return Task.FromResult(
            _entries.TryGetValue(SynthesizedSpeechCacheKey.For(voiceName, text), out var cached)
                ? cached
                : null);
    }

    public Task SetAsync(
        string voiceName,
        string text,
        SynthesizedSpeech speech,
        CancellationToken cancellationToken = default)
    {
        if (!SynthesizedSpeechCacheKey.IsCacheable(text)
            || speech.AudioContent.Length > SynthesizedSpeechCacheKey.MaxCacheableAudioBytes)
            return Task.CompletedTask;

        var key = SynthesizedSpeechCacheKey.For(voiceName, text);
        if (_entries.Count >= MaxEntries && !_entries.ContainsKey(key))
        {
            var victim = _entries.Keys.FirstOrDefault();
            if (victim is not null)
                _entries.TryRemove(victim, out _);
        }

        _entries[key] = speech;
        return Task.CompletedTask;
    }
}
