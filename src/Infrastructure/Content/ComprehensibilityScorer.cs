using System.Text.RegularExpressions;
using Domain.Content;

namespace Infrastructure.Content;

public sealed partial class ComprehensibilityScorer : IComprehensibilityScorer
{
    public ComprehensibilityResult Score(
        string text,
        IEnumerable<string> knownLemmas,
        IEnumerable<string> topicLemmas)
    {
        var understood = knownLemmas.Concat(topicLemmas)
            .Select(NormalizeLemma)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tokens = TokenRegex().Matches(text).Select(match => NormalizeLemma(match.Value)).ToArray();
        var comprehensible = tokens.Count(understood.Contains);
        var unknown = tokens.Where(token => !understood.Contains(token))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var score = tokens.Length == 0 ? 0 : Math.Round(100d * comprehensible / tokens.Length, 1);
        return new ComprehensibilityResult(score, tokens.Length, comprehensible, unknown);
    }

    private static string NormalizeLemma(string value)
    {
        var word = value.Trim().ToLowerInvariant();
        if (word is "went") return "go";
        if (word is "bought") return "buy";
        if (word.EndsWith("ies", StringComparison.Ordinal) && word.Length > 3)
            return word[..^3] + "y";
        if (word.EndsWith("s", StringComparison.Ordinal) && word.Length > 3)
            return word[..^1];
        return word;
    }

    [GeneratedRegex("[A-Za-z]+(?:'[A-Za-z]+)?")]
    private static partial Regex TokenRegex();
}