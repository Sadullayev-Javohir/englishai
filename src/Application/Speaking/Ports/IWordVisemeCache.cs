using Application.Speaking.Models;

namespace Application.Speaking.Ports;

/// <summary>
/// Caches the synthesized speech (reference audio + red-lips viseme/SVG track) for a single
/// word, so the pronunciation-detail screen synthesizes each word at most once (docs/development-guide.md
/// rule 10 - TTS caching). The same word produces the same audio and mouth animation for
/// every learner. The audio is cached too: the detail screen plays the Azure reference
/// voice, which the browser's built-in speech synthesis cannot be relied on for (no voices
/// are installed on many Linux/Chromium clients, so it stays silent).
/// </summary>
public interface IWordVisemeCache
{
    bool TryGet(string word, out SynthesizedSpeech speech);

    void Set(string word, SynthesizedSpeech speech);
}
