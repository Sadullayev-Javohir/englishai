namespace Domain.Vocabulary;

/// <summary>
/// Why a written-sentence mini-test (<see cref="MiniTestType.WrittenUsage"/>) passed or failed
/// (PROJECT-SPEC B.1). A structured code, never free LLM prose (docs/development-guide.md rule 11) - the client
/// resolves it to a vetted Uzbek explanation from the content store.
/// </summary>
public enum WordUsageReasonCode
{
    /// <summary>The sentence correctly and clearly uses the target word.</summary>
    Correct = 0,

    /// <summary>The target word (or a natural inflection of it) does not appear in the sentence.</summary>
    WordNotUsed = 1,

    /// <summary>The word appears but is used with the wrong meaning/part of speech.</summary>
    WrongMeaning = 2,

    /// <summary>The submission is too short/trivial to judge as a real sentence.</summary>
    TooShort = 3,

    /// <summary>The submission does not read as English.</summary>
    NotEnglish = 4,
}
