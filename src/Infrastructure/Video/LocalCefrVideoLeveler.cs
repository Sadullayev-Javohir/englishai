using Application.Video.Ports;
using Domain.Assessment;

namespace Infrastructure.Video;

/// <summary>
/// Deterministic local stand-in for the LLM video leveler. Estimates CEFR from simple
/// readability proxies (average word length and the share of long words), so the
/// ingestion pipeline produces a stable level without an LLM key. Replace with
/// <see cref="LlmCefrVideoLeveler"/> when a Gemini key is configured.
/// </summary>
public sealed class LocalCefrVideoLeveler : ICefrVideoLeveler
{
    public Task<CefrLevel> EstimateLevelAsync(string transcript, CancellationToken cancellationToken = default)
    {
        var words = transcript.Split(
            new[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?' },
            StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            return Task.FromResult(CefrLevel.A2);

        var averageLength = words.Average(w => w.Length);
        var longWordRatio = (double)words.Count(w => w.Length >= 8) / words.Length;

        // A blended readability score (0-100) mapped to a CEFR band via the shared scale.
        var score = (averageLength * 9) + (longWordRatio * 120);
        var level = CefrLevelExtensions.FromScore(Math.Clamp(score, 0, 100));

        return Task.FromResult(level);
    }
}
