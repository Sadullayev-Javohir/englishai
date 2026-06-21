namespace Domain.Vocabulary;

/// <summary>
/// The next spaced-repetition checkpoint for a learned word (PROJECT-SPEC B.1, the
/// 3/7/21-day rule). A word climbs the ladder on each successful review and falls
/// back to <see cref="Day3"/> on a failed one; <see cref="Mastered"/> is the terminal
/// "long-term memory" state, followed by maintenance reviews.
/// </summary>
public enum ReviewStage
{
    /// <summary>Review due 3 days after the word was learned.</summary>
    Day3 = 0,

    /// <summary>Review due 7 days after the word was learned.</summary>
    Day7 = 1,

    /// <summary>Final review due 21 days after the word was learned.</summary>
    Day21 = 2,

    /// <summary>Moved to long-term memory; subsequent reviews maintain recall.</summary>
    Mastered = 3
}
