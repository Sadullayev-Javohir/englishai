using Domain.Learning;

namespace Application.Grammar.Models;

/// <summary>
/// Maps a learning-spine grammar focus code (e.g. "past-simple", "articles") to the
/// <see cref="ErrorCategory"/> bucket the error heatmap aggregates (PROJECT-SPEC C.7). Each topic's
/// grammar lesson is generated from its focus code, so a wrong answer is still reported against the
/// Uzbek-learner difficulty category it belongs to. The match is by keyword on the kebab-case code,
/// falling back to <see cref="ErrorCategory.Other"/> for codes that don't map to a single bucket.
/// </summary>
public static class GrammarFocusCategory
{
    public static ErrorCategory For(string? focusCode)
    {
        if (string.IsNullOrWhiteSpace(focusCode))
            return ErrorCategory.Other;

        var code = focusCode.Trim().ToLowerInvariant();

        if (code.Contains("article"))
            return ErrorCategory.Articles;
        if (code.Contains("preposition"))
            return ErrorCategory.Prepositions;
        if (code.Contains("gerund") || code.Contains("infinitive"))
            return ErrorCategory.GerundInfinitive;
        if (code.Contains("modal"))
            return ErrorCategory.Modals;
        if (code.Contains("agreement"))
            return ErrorCategory.SubjectVerbAgreement;
        if (code.Contains("question") || code.Contains("interrogative"))
            return ErrorCategory.WordOrder;
        if (code.Contains("inversion") || code.Contains("order") || code.Contains("fronting") ||
            code.Contains("cleft") || code.Contains("emphas") || code.Contains("emphatic"))
            return ErrorCategory.WordOrder;

        // Tense / aspect / mood - the large "verb" family (simple, perfect, continuous, past,
        // future, conditional, subjunctive, going-to, will, used-to, causative, reported speech …).
        if (code.Contains("tense") || code.Contains("simple") || code.Contains("perfect") ||
            code.Contains("continuous") || code.Contains("past") || code.Contains("future") ||
            code.Contains("conditional") || code.Contains("subjunctive") || code.Contains("will") ||
            code.Contains("going-to") || code.Contains("used-to") || code.Contains("causative") ||
            code.Contains("passive") || code.Contains("reporting") || code.Contains("reported") ||
            code.Contains("wish") || code.Contains("hypothetical") || code.Contains("be"))
            return ErrorCategory.VerbTense;

        return ErrorCategory.Other;
    }
}
