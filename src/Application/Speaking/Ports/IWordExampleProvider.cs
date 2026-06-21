namespace Application.Speaking.Ports;

/// <summary>
/// Resolves a verified Uzbek example sentence for a given English word (docs/development-guide.md
/// rule 11: Uzbek UI text comes only from vetted content, never free LLM generation).
/// Returns null when no curated example exists for the word, so the frontend can
/// render the 3D example card conditionally.
/// </summary>
public interface IWordExampleProvider
{
    /// <summary>Returns the curated Uzbek example sentence, or <c>null</c> if unknown.</summary>
    string? GetExampleSentenceUz(string word);
}
