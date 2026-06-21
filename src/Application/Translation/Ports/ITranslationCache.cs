namespace Application.Translation.Ports;

/// <summary>
/// A process-wide cache of English→Uzbek sentence translations. Translations of teaching text are
/// stable, so caching them means each distinct sentence is translated by the LLM at most once
/// (docs/development-guide.md rule 10) - sentences repeat heavily across lessons (shared prompts, instructions),
/// giving a high hit rate. Keyed by the exact normalized English text plus the requested level.
/// </summary>
public interface ITranslationCache
{
    /// <summary>Returns the cached Uzbek translation for <paramref name="key"/>, or null on a miss.</summary>
    bool TryGet(string key, out string translation);

    /// <summary>Caches the Uzbek <paramref name="translation"/> under <paramref name="key"/>.</summary>
    void Set(string key, string translation);
}
