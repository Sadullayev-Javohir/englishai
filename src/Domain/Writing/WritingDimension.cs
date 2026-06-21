namespace Domain.Writing;

/// <summary>
/// The four assessment dimensions for a writing submission (PROJECT-SPEC G.3), aligned to
/// CEFR written-assessment standards. The LLM scores each one independently 1-5; the
/// human-readable Uzbek labels/feedback live in the content layer (docs/development-guide.md rule 11).
/// </summary>
public enum WritingDimension
{
    /// <summary>Did the writer answer the task/prompt?</summary>
    TaskAchievement = 1,

    /// <summary>Are ideas connected and paragraphs logical?</summary>
    Coherence = 2,

    /// <summary>Word choice and repetition (lexical resource).</summary>
    LexicalResource = 3,

    /// <summary>Number and type of grammar errors (grammatical accuracy).</summary>
    GrammaticalAccuracy = 4
}
