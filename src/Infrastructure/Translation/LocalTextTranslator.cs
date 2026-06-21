using Application.Translation.Ports;
using Domain.Assessment;

namespace Infrastructure.Translation;

/// <summary>
/// Honest no-op <see cref="ITextTranslator"/> used when no LLM key is configured (same gating pattern
/// as the other Local* stand-ins). It cannot actually translate, so it returns <c>null</c> - the UI
/// then shows an honest "translation unavailable" state rather than fabricated Uzbek (rules 8, 11).
/// Real translation comes from <see cref="LlmTextTranslator"/> in production.
/// </summary>
public sealed class LocalTextTranslator : ITextTranslator
{
    public Task<string?> TranslateAsync(
        string englishText, CefrLevel? level = null, TranslationContext? context = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
