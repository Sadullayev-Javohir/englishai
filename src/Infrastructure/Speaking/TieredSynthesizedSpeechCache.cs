using Application.Speaking.Models;
using Application.Speaking.Ports;

namespace Infrastructure.Speaking;

/// <summary>
/// Process-local cache in front of the shared one. The handful of stock lines every session opens
/// with are read constantly; serving them from memory skips a Redis round-trip and a base64 decode
/// per turn, while the shared cache still stops a second app instance from paying Azure again.
/// </summary>
public sealed class TieredSynthesizedSpeechCache(
    ISynthesizedSpeechCache local,
    ISynthesizedSpeechCache shared) : ISynthesizedSpeechCache
{
    public async Task<SynthesizedSpeech?> GetAsync(
        string voiceName, string text, CancellationToken cancellationToken = default)
    {
        var cached = await local.GetAsync(voiceName, text, cancellationToken);
        if (cached is not null)
            return cached;

        cached = await shared.GetAsync(voiceName, text, cancellationToken);
        if (cached is not null)
            await local.SetAsync(voiceName, text, cached, cancellationToken);

        return cached;
    }

    public async Task SetAsync(
        string voiceName,
        string text,
        SynthesizedSpeech speech,
        CancellationToken cancellationToken = default)
    {
        await local.SetAsync(voiceName, text, speech, cancellationToken);
        await shared.SetAsync(voiceName, text, speech, cancellationToken);
    }
}
