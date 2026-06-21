namespace Infrastructure.Common;

/// <summary>
/// Guards generated content against any non-target language - most importantly stray Chinese / CJK
/// (the classic leak from budget models like tencent/hy3), but also Cyrillic, Korean, Japanese,
/// Arabic, and any other non-English/non-Uzbek script. docs/development-guide.md rule 11 requires every Uzbek-facing
/// string to be vetted/sanctioned and the target-language English text to stay English - a Chinese
/// (or any off-language) character inside a passage or word list is a hard defect, never a "close
/// enough" answer. When any forbidden script is detected the whole payload is rejected (the caller
/// leaves the topic pending and it is re-filled on the next backfill run), so a contaminated reply
/// never reaches the database (rules 8, 11).
///
/// Target languages are STRICTLY English (Latin) and Uzbek (Latin script: 'o', 'g\'', 'sh', 'ch',
/// 'ng', plus apostrophe). Cyrillic is NOT allowed even in "uz" fields - Uzbek content is authored in
/// the Latin alphabet only, so any Cyrillic (ў, қ, ғ, ҳ …) is treated as a contamination.
/// </summary>
public static class ContentLanguageGuard
{
    /// <summary>
    /// Returns true when <paramref name="text"/> is strictly English (Latin) or Uzbek (Latin script).
    /// Any CJK / Cyrillic / Hangul / Kana / Arabic / full-width / other non-target script is rejected.
    /// Latin Extended-A/B characters used by other European languages (é, ñ, ü, ß …) are also rejected
    /// so the content stays strictly English+Uzbek (docs/development-guide.md rule 11, user directive).
    /// </summary>
    public static bool IsClean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        foreach (var ch in text)
        {
            // Uzbek Latin uses ASCII digraphs/apostrophes (o', g', sh, ch, ng), so every non-ASCII
            // code point is contamination rather than a target-language requirement.
            if (ch > 127)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Rewrites the typographic look-alikes an LLM habitually emits into the plain ASCII the target
    /// languages are authored in, so a cosmetic character never costs the learner a whole answer.
    /// <see cref="IsClean"/> rejects every code point above 127, which is correct for SCRIPT
    /// contamination (CJK, Cyrillic, Arabic - a hard defect under docs/development-guide.md rule 11) but far too harsh
    /// for punctuation: a model that writes a perfectly good Uzbek sentence and ends it with a typographic
    /// ellipsis, or spells o'z as oʻz with the Uzbek okina (U+02BB - typographically the CORRECT letter,
    /// just not the ASCII this codebase standardises on), had its entire reply thrown away and the
    /// learner saw "AI hozir javob bera olmadi" (observed 2026-07-28).
    ///
    /// Only characters with an exact, meaning-preserving ASCII equivalent are mapped. Letters are NOT
    /// transliterated - accented Latin (é, ñ, ü, ß …) stays non-ASCII and is still rejected downstream,
    /// because those signal a third language rather than a typography preference (rule 11, user
    /// directive). Callers pass the result to <see cref="IsClean"/>; normalizing does not bypass the
    /// guard, it just stops the guard from firing on punctuation.
    /// </summary>
    public static string? NormalizeToAscii(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Fast path: the overwhelming majority of replies are already plain ASCII.
        var needsWork = false;
        foreach (var ch in text)
        {
            if (ch > 127)
            {
                needsWork = true;
                break;
            }
        }

        if (!needsWork)
            return text;

        // Every mapping is a punctuation/typography look-alike with an exact ASCII equivalent, written
        // as an escape rather than a literal because several of these are indistinguishable on sight.
        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            switch (ch)
            {
                // Apostrophes and single quotes. U+02BB (okina) and U+02BC (tutuq belgisi) are the
                // typographically correct Uzbek letters for o'/g' and the glottal stop - the single most
                // common non-ASCII character in generated Uzbek, and exactly what this codebase spells
                // as a plain ASCII apostrophe.
                case '\u2018' or '\u2019' or '\u201A' or '\u201B'
                    or '\u02B9' or '\u02BB' or '\u02BC' or '\u02BD' or '\u02C8'
                    or '\u00B4' or '\u2032':
                    builder.Append('\'');
                    break;

                // Double quotes, including the guillemets Russian-influenced text carries over.
                case '\u201C' or '\u201D' or '\u201E' or '\u201F'
                    or '\u00AB' or '\u00BB' or '\u2033':
                    builder.Append('"');
                    break;

                // Hyphens, dashes and the minus sign.
                case '\u2010' or '\u2011' or '\u2012' or '\u2013'
                    or '\u2014' or '\u2015' or '\u2212':
                    builder.Append('-');
                    break;

                case '\u2026':
                    builder.Append("...");
                    break;

                // Non-breaking and typographic spaces.
                case '\u00A0' or '\u2007' or '\u2008' or '\u2009'
                    or '\u200A' or '\u202F' or '\u205F' or '\u3000':
                    builder.Append(' ');
                    break;

                // Zero-width marks carry no meaning in this content, so there is nothing to map them
                // to - drop them rather than leave a character the guard would reject.
                case '\u200B' or '\u200C' or '\u200D' or '\uFEFF':
                    break;

                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Heuristic quality gate for a dynamically-generated Uzbek translation of an English example
    /// sentence (e.g. a grammar lesson's <c>examples[].uz</c> - docs/development-guide.md rule 11: the English
    /// example is itself dynamic, so its translation can't be a fixed lookup table; this is the
    /// pragmatic "second-pass check" fallback instead). Rejects the classic degenerate LLM outputs:
    /// empty text, non-Latin script (Cyrillic Uzbek included - rule 11 requires Latin script), an
    /// exact copy of the English sentence (the model just echoed it back instead of translating),
    /// and a translation so short relative to the English that it can't plausibly carry the same
    /// meaning. It cannot verify the translation is semantically correct - that would need a second
    /// LLM call, which is out of scope for a lightweight guard - only that it isn't obviously broken.
    /// </summary>
    public static bool IsPlausibleTranslation(string? english, string? uzbek)
    {
        if (string.IsNullOrWhiteSpace(uzbek))
            return false;
        if (!IsClean(uzbek))
            return false;

        var trimmedUz = uzbek.Trim();
        if (trimmedUz.Length < 2)
            return false;

        var trimmedEn = english?.Trim() ?? string.Empty;
        if (trimmedEn.Length > 0 && string.Equals(trimmedUz, trimmedEn, StringComparison.OrdinalIgnoreCase))
            return false;

        // A real sentence translation is rarely a tiny fraction of the English sentence's length
        // (short single-word glosses are fine for word lists, but grammar examples pair full
        // sentences with a full Uzbek meaning).
        if (trimmedEn.Length >= 12 && trimmedUz.Length < trimmedEn.Length / 4)
            return false;

        return true;
    }
}
