using System.Text.RegularExpressions;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;

namespace Infrastructure.Vocabulary;

/// <summary>
/// Deterministic, dependency-free grader for the <see cref="MiniTestType.WrittenUsage"/> SRS
/// mini-test (PROJECT-SPEC B.1). Used for dev/tests and whenever no LLM key is configured, and as
/// the offline fallback behind <see cref="ResilientWordUsageAssessor"/> (same config-gating
/// pattern as <c>Infrastructure.Writing.LocalWritingAssessor</c>, docs/development-guide.md rules 10/15). It checks
/// two real, honest signals - the submission reads as a real sentence (not a one-word or trivial
/// answer) and the target word (or a simple inflection of it) actually appears in it - rather than
/// fabricating a pass (rules 8, 11).
/// </summary>
public sealed partial class LocalWordUsageAssessor : IWordUsageAssessor
{
    /// <summary>A real sentence needs at least this many words beyond the target word itself.</summary>
    private const int MinWordCount = 3;

    [GeneratedRegex(@"[A-Za-z']+")]
    private static partial Regex WordToken();

    public Task<WordUsageAssessment> AssessAsync(
        string word, string translation, string submittedSentence, CancellationToken cancellationToken)
    {
        var tokens = WordToken().Matches(submittedSentence).Select(m => m.Value).ToList();

        if (tokens.Count < MinWordCount)
            return Task.FromResult(WordUsageAssessment.Fail(WordUsageReasonCode.TooShort));

        var targetStem = Stem(word);
        var usesWord = tokens.Any(t => string.Equals(Stem(t), targetStem, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(usesWord
            ? WordUsageAssessment.Pass()
            : WordUsageAssessment.Fail(WordUsageReasonCode.WordNotUsed));
    }

    /// <summary>
    /// A crude, dependency-free stem: strips a common English inflectional suffix so "runs"/
    /// "running"/"boxes" match their base word "run"/"box". Not a real morphological analyzer -
    /// just enough to accept the natural inflections a learner would actually write, without
    /// pulling in an NLP dependency for a single fallback check.
    /// </summary>
    private static string Stem(string token)
    {
        var t = token.Trim().ToLowerInvariant();
        foreach (var suffix in new[] { "ing", "edly", "ed", "ies", "es", "s", "ly" })
        {
            if (t.Length > suffix.Length + 2 && t.EndsWith(suffix, StringComparison.Ordinal))
                return t[..^suffix.Length];
        }
        return t;
    }
}
