namespace Application.Grammar.Ports;

/// <summary>
/// Resolves vetted Uzbek content for the grammar module from the content store
/// (docs/development-guide.md rule 11) - never free-generated at runtime. Maps a lesson's explanation
/// code to its Uzbek rule explanation (step 2), an exercise's hint code to its Uzbek
/// hint (shown only for a wrong answer), and a common-mistake code to its vetted Uzbek
/// explanation (step 2's "Xatolar" list - see <see cref="Infrastructure.Grammar.LlmGrammarContentGenerator"/>,
/// which asks the LLM for a mistake CODE, never free Uzbek prose, exactly like the Writing
/// module's <c>IWritingContentProvider</c>).
/// </summary>
public interface IGrammarContentProvider
{
    /// <summary>
    /// The Uzbek rule explanation for an explanation code, or <c>null</c> if the code is
    /// unknown or not set.
    /// </summary>
    string? GetExplanation(string? explanationCode);

    /// <summary>
    /// The Uzbek hint for an exercise's hint code, or <c>null</c> if the code is unknown
    /// or not set.
    /// </summary>
    string? GetHint(string? hintCode);

    /// <summary>
    /// The vetted Uzbek explanation of a common-mistake code (e.g. <c>missing_article</c>), or
    /// <c>null</c> if the code is unknown/not set - callers must never fall back to raw LLM text
    /// for an unresolved code (rule 11).
    /// </summary>
    string? GetMistakeExplanation(string? mistakeCode);
}
