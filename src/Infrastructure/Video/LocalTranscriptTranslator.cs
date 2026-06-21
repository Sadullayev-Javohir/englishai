using Application.Video.Models;
using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Deterministic local stand-in for the LLM transcript translator. It produces no Uzbek text
/// at all - translating real English into Uzbek is exactly the kind of dynamic content that
/// must not be fabricated without a real model (docs/development-guide.md rules 8, 11). So without an LLM key
/// the transcript simply stays in its honest, English-only "pending" state. Replace with
/// <see cref="LlmTranscriptTranslator"/> when a Gemini key is configured.
/// </summary>
public sealed class LocalTranscriptTranslator : IVideoTranscriptTranslator
{
    public Task<TranscriptTranslation> TranslateAsync(
        IReadOnlyList<TranscriptLine> englishLines, CancellationToken cancellationToken = default) =>
        Task.FromResult(TranscriptTranslation.Empty);
}
