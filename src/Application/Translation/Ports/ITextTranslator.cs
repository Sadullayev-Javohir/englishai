using Domain.Assessment;

namespace Application.Translation.Ports;

/// <summary>
/// Translates a single piece of real English teaching text (a sentence, prompt or short paragraph)
/// into natural Uzbek for an Uzbek learner of English. This is the sanctioned dynamic-Uzbek path of
/// docs/development-guide.md rule 11: the model translates an existing English source it is shown - it never invents
/// UI copy - and the output is cached (<see cref="ITranslationCache"/>) so each distinct sentence is
/// paid for at most once (rule 10). Implementations must return <c>null</c> (never throw, never
/// fabricate) when no translator is configured or a call fails, so the UI can show an honest
/// "translation unavailable" state rather than fake text (rules 8, 11).
/// </summary>
public interface ITextTranslator
{
    /// <summary>
    /// The natural Uzbek translation of <paramref name="englishText"/>, or <c>null</c> when
    /// translation is unavailable. <paramref name="level"/> lets the wording match the learner's
    /// CEFR level (simpler Uzbek for lower levels) when known.
    /// </summary>
    Task<string?> TranslateAsync(
        string englishText, CefrLevel? level = null, TranslationContext? context = null,
        CancellationToken cancellationToken = default);
}

public sealed record TranslationContext(
    string? Speaker = null,
    string? Topic = null,
    IReadOnlyList<string>? PreviousTurns = null);
