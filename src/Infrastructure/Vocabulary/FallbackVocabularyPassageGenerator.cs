using Application.Vocabulary.Models;
using Application.Vocabulary.Ports;
using Domain.Assessment;

namespace Infrastructure.Vocabulary;

/// <summary>
/// Composite <see cref="IVocabularyPassageGenerator"/> that runs a primary (LLM) generator first and
/// falls back to a deterministic local stand-in whenever the primary returns no usable content
/// (no API key, rate-limit, invalid key, offline). This guarantees vocabulary topic pages always
/// show a passage and words instead of staying permanently "pending" (rules 8, 11).
/// </summary>
public sealed class FallbackVocabularyPassageGenerator : IVocabularyPassageGenerator
{
    private readonly IVocabularyPassageGenerator _primary;
    private readonly IVocabularyPassageGenerator _fallback;

    public FallbackVocabularyPassageGenerator(
        IVocabularyPassageGenerator primary,
        IVocabularyPassageGenerator fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public async Task<GeneratedTopicContent> GenerateAsync(
        string title, CefrLevel level, int targetWordCount, CancellationToken cancellationToken = default)
    {
        var result = await _primary.GenerateAsync(title, level, targetWordCount, cancellationToken);
        if (!result.HasContent)
            return await _fallback.GenerateAsync(title, level, targetWordCount, cancellationToken);

        return result;
    }
}
