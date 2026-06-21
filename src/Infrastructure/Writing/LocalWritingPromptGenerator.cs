using Application.Writing.Content;
using Application.Writing.Models;
using Application.Writing.Ports;
using Domain.Assessment;

namespace Infrastructure.Writing;

/// <summary>
/// Deterministic local stand-in for <see cref="IWritingPromptGenerator"/>, used when no LLM key is
/// configured so the Writing module runs and is testable offline (same gating pattern as
/// <c>LocalReadingContentGenerator</c>). It picks a CEFR-appropriate genre for the topic from
/// <see cref="WritingGenreCatalog"/> (varied per topic, not one fixed task type) and fills its
/// prompt template and guidance. Real, richer topic-specific prompts come from
/// <see cref="HermesWritingPromptGenerator"/> in production.
/// </summary>
public sealed class LocalWritingPromptGenerator : IWritingPromptGenerator
{
    public Task<GeneratedWritingPrompt> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default)
    {
        var genre = WritingGenreCatalog.ForTopic(title, level);
        var prompt = genre.PromptTemplate.Replace("{title}", title, StringComparison.Ordinal);

        return Task.FromResult(new GeneratedWritingPrompt(prompt, genre.Guidance));
    }
}
