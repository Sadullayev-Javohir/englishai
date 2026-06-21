using Application.Video.Models;

namespace Application.Video.Ports;

/// <summary>
/// Translates a video's real English transcript into Uzbek (per line) and extracts a glossary
/// of its notable words (PROJECT-SPEC B.3, Bosqich 3). This is the sanctioned dynamic-Uzbek
/// path of docs/development-guide.md rule 11: the model translates an existing English source (it never invents
/// UI copy), the output is validated, and the result is cached per video. Implementations must
/// return <see cref="TranscriptTranslation.Empty"/> (never throw, never fabricate) when no
/// translator is configured or a call fails, so the transcript stays in its honest, untranslated
/// "pending" state (rules 8, 11).
/// </summary>
public interface IVideoTranscriptTranslator
{
    /// <summary>
    /// The Uzbek translation for each English line (parallel to <paramref name="englishLines"/>)
    /// plus a glossary of notable content words; <see cref="TranscriptTranslation.Empty"/> when
    /// translation is unavailable.
    /// </summary>
    Task<TranscriptTranslation> TranslateAsync(
        IReadOnlyList<TranscriptLine> englishLines, CancellationToken cancellationToken = default);
}
