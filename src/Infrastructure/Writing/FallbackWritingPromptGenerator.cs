using Application.Writing.Models;
using Application.Writing.Ports;
using Domain.Assessment;

namespace Infrastructure.Writing;

public sealed class FallbackWritingPromptGenerator : IWritingPromptGenerator
{
    private readonly IWritingPromptGenerator _primary;
    private readonly IWritingPromptGenerator _fallback;

    public FallbackWritingPromptGenerator(
        IWritingPromptGenerator primary,
        IWritingPromptGenerator fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public async Task<GeneratedWritingPrompt> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<string>? targetWords = null,
        CancellationToken cancellationToken = default)
    {
        GeneratedWritingPrompt result;
        try
        {
            result = await _primary.GenerateAsync(title, level, targetWords, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result = GeneratedWritingPrompt.Empty;
        }

        if (!result.HasContent)
            return await _fallback.GenerateAsync(title, level, targetWords, cancellationToken);

        return result;
    }
}
