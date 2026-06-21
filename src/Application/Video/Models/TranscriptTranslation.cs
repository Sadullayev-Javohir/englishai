namespace Application.Video.Models;

/// <summary>
/// One content word from a video's transcript paired with a short vetted Uzbek meaning,
/// shown when a learner taps/hovers an unknown word (PROJECT-SPEC B.3, Bosqich 3). The
/// English word is real (from the captions); the Uzbek meaning is dynamic translated
/// content - the unavoidable-dynamic-text exception of docs/development-guide.md rule 11 (translating a
/// real source, validated, never free-invented UI copy).
/// </summary>
public sealed record WordGloss(string Word, string UzbekMeaning);

/// <summary>
/// The Uzbek translation layer for a fetched transcript: a per-line Uzbek translation
/// (parallel to the input lines, <c>null</c> where the line could not be translated) plus a
/// glossary of the transcript's notable content words. Producers must return an empty result
/// (no fabricated text) on any failure, so the player keeps its honest "pending" state
/// (docs/development-guide.md rules 8, 11).
/// </summary>
public sealed record TranscriptTranslation(
    IReadOnlyList<string?> LineTranslations,
    IReadOnlyList<WordGloss> Glossary)
{
    /// <summary>An empty translation - used as the honest no-op when no LLM is configured or a call fails.</summary>
    public static readonly TranscriptTranslation Empty =
        new(Array.Empty<string?>(), Array.Empty<WordGloss>());

    public bool IsEmpty => LineTranslations.Count == 0 && Glossary.Count == 0;
}
