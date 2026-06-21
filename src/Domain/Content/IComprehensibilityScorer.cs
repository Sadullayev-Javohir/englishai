namespace Domain.Content;

public sealed record ComprehensibilityResult(
    double Score,
    int TotalTokens,
    int ComprehensibleTokens,
    IReadOnlyList<string> UnknownLemmas);

public interface IComprehensibilityScorer
{
    ComprehensibilityResult Score(
        string text,
        IEnumerable<string> knownLemmas,
        IEnumerable<string> topicLemmas);
}