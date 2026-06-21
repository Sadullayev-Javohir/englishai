using Domain.Speaking;

namespace Application.Speaking.Models;

/// <summary>Synthesized speech: audio bytes, synchronized visemes, and whether audio is a spoken voice.</summary>
public sealed record SynthesizedSpeech(
    byte[] AudioContent,
    VisemeSequence Visemes,
    bool IsNaturalVoice = true,
    IReadOnlyList<SpeechWordTiming>? WordTimings = null)
{
    public IReadOnlyList<SpeechWordTiming> Timings { get; } =
        WordTimings ?? Array.Empty<SpeechWordTiming>();
}

public sealed record SpeechWordTiming(
    string Text,
    int TextOffset,
    int WordLength,
    TimeSpan AudioOffset,
    TimeSpan Duration);

/// <summary>
/// Visual representation of one IPA phoneme: the SVG mouth-shape id, the Azure viseme
/// id (0..21) used to drive the animated 2D mouth, whether the sound is typically hard
/// for Uzbek speakers, and an optional Uzbek tip code (resolved to text via the
/// feedback template provider).
/// </summary>
public sealed record PhonemeVisual(
    string Phoneme,
    string SvgId,
    int VisemeId,
    bool IsHardForUzbek,
    string? TipCode);

/// <summary>Phonetic breakdown of a word: its IPA transcription and ordered phonemes.</summary>
public sealed record WordPhonetics(string Word, string Ipa, IReadOnlyList<string> Phonemes);
