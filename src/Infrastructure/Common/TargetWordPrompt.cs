namespace Infrastructure.Common;

/// <summary>
/// Renders the optional "target words" line shared by every skill's content generator. The unified
/// topic spine teaches a set of vocabulary words per topic (the <c>Vocabulary</c> module); the other
/// skills (reading, grammar, writing, listening, speaking) are asked to reuse those same words so the
/// learner meets each word repeatedly, across skills, in context - which is what moves a word from
/// recognised to usable (docs/development-guide.md mission). The reuse is intentionally SOFT: the model weaves the
/// words in where they fit naturally rather than forcing every one, so content stays natural.
/// </summary>
public static class TargetWordPrompt
{
    /// <summary>
    /// A prompt fragment listing the topic's vocabulary words to reuse, or an empty string when there
    /// are none (e.g. the vocabulary topic has not been filled yet) - in which case the generator
    /// behaves exactly as before. Append the result to a generator's user prompt.
    /// </summary>
    public static string Line(IEnumerable<string>? targetWords)
    {
        var words = targetWords?
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => w.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (words is null || words.Count == 0)
            return string.Empty;

        return "\nTARGET WORDS (reuse these naturally where they fit; do NOT force all of them): "
            + string.Join(", ", words);
    }
}
