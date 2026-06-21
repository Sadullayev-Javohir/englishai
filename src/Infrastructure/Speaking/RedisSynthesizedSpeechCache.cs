using Application.Speaking.Models;
using Application.Speaking.Ports;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Speaking;

/// <summary>
/// Shared synthesis cache. Tutor openings repeat across learners, not just within one process, so
/// this belongs in Redis rather than in memory - a second app instance would otherwise pay Azure
/// again for lines the first instance already synthesized.
/// </summary>
public sealed class RedisSynthesizedSpeechCache(IRedisConnectionProvider redis) : ISynthesizedSpeechCache
{
    /// <summary>
    /// Long, because the value is immutable for its key: the same voice and the same text always
    /// synthesize to the same audio. Only a voice-model change invalidates it, and that changes the
    /// voice name (and therefore the key) anyway.
    /// </summary>
    private static readonly TimeSpan TimeToLive = TimeSpan.FromDays(30);

    private readonly IDatabase _database = redis.Get(RedisWorkload.Cache).GetDatabase();

    public async Task<SynthesizedSpeech?> GetAsync(
        string voiceName, string text, CancellationToken cancellationToken = default)
    {
        if (!SynthesizedSpeechCacheKey.IsCacheable(text))
            return null;

        try
        {
            var value = await _database
                .StringGetAsync(Key(voiceName, text))
                .WaitAsync(cancellationToken);
            return value.HasValue ? SynthesizedSpeechEnvelope.FromJson(value.ToString()) : null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A cache is an optimization: an unreachable Redis must read as a miss, never as a
            // failed learner request.
            return null;
        }
    }

    public async Task SetAsync(
        string voiceName,
        string text,
        SynthesizedSpeech speech,
        CancellationToken cancellationToken = default)
    {
        if (!SynthesizedSpeechCacheKey.IsCacheable(text)
            || speech.AudioContent.Length > SynthesizedSpeechCacheKey.MaxCacheableAudioBytes)
            return;

        try
        {
            var payload = SynthesizedSpeechEnvelope.From(speech).ToJson();
            await _database
                .StringSetAsync(Key(voiceName, text), payload, TimeToLive)
                .WaitAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }
    }

    private string Key(string voiceName, string text) =>
        redis.Key(RedisWorkload.Cache, "speech", $"tts:{SynthesizedSpeechCacheKey.For(voiceName, text)}");
}
