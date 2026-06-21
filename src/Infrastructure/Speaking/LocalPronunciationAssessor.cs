using Application.Speaking.Ports;
using Domain.Speaking;
using Infrastructure.Common;

namespace Infrastructure.Speaking;

/// <summary>
/// Deterministic local stand-in for Azure Pronunciation Assessment. Scores each
/// word reproducibly from a stable hash, penalizing sounds that are typically hard
/// for Uzbek speakers ("th", "r"). Replace with an Azure adapter (keys required).
/// </summary>
public sealed class LocalPronunciationAssessor : IPronunciationAssessor
{
    public Task<PronunciationResult> AssessAsync(
        byte[] audioContent,
        string referenceText,
        CancellationToken cancellationToken = default)
    {
        var tokens = referenceText
            .Split(new[] { ' ', '\t', '\n', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        var words = tokens.Select(ScoreWord).ToList();
        var overall = words.Count == 0 ? 0 : words.Average(w => w.AccuracyScore);
        var fluency = Math.Min(100, overall + 3);

        var result = new PronunciationResult(
            Math.Round(overall, 1),
            Math.Round(overall, 1),
            Math.Round(fluency, 1),
            completenessScore: 100,
            words,
            isAuthentic: false);

        return Task.FromResult(result);
    }

    private static WordPronunciation ScoreWord(string word)
    {
        var lower = word.ToLowerInvariant();
        double score = 60 + StableHash.Of(lower) % 39; // 60..98
        if (lower.Contains("th") || lower.Contains('r'))
            score = Math.Max(20, score - 25);

        var errorType = score < SpeakingScoreThresholds.WordNeedsPractice
            ? PronunciationErrorType.Mispronunciation
            : PronunciationErrorType.None;

        return new WordPronunciation(word, Math.Round(score, 1), errorType);
    }
}
