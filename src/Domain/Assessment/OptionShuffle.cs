namespace Domain.Assessment;

/// <summary>
/// Deterministically shuffles a question's answer options per (session, question).
///
/// Two goals: different learners see the options (and the correct answer) in a
/// different position, which defeats answer-key sharing and pattern memorization;
/// and the permutation is reproducible from the ids alone, so the server can shuffle
/// at serve time and un-shuffle the learner's chosen index at grade time without
/// storing any per-question state.
///
/// <see cref="Order"/> returns a map from display position to original option index:
/// <c>displayedOptions[i] = originalOptions[Order(...)[i]]</c>.
/// </summary>
public static class OptionShuffle
{
    /// <summary>
    /// The permutation for <paramref name="optionCount"/> options under a seed derived
    /// from the session and question ids. <c>result[displayPosition] = originalIndex</c>.
    /// </summary>
    public static int[] Order(Guid sessionId, Guid questionId, int optionCount)
    {
        var order = new int[optionCount];
        for (var i = 0; i < optionCount; i++)
            order[i] = i;

        var rng = new Random(Seed(sessionId, questionId));
        // Fisher-Yates.
        for (var i = optionCount - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        return order;
    }

    /// <summary>The original option index the learner actually chose, given the index
    /// they clicked in the shuffled display.</summary>
    public static int ToOriginalIndex(Guid sessionId, Guid questionId, int optionCount, int displayedIndex)
    {
        if (displayedIndex < 0 || displayedIndex >= optionCount)
            return displayedIndex; // out of range; let the caller's correctness check fail it.

        return Order(sessionId, questionId, optionCount)[displayedIndex];
    }

    private static int Seed(Guid sessionId, Guid questionId)
    {
        // Fold both ids' bytes into a stable, process-independent seed (HashCode is
        // randomized per process, which would make the shuffle non-reproducible).
        var seed = 17;
        foreach (var b in sessionId.ToByteArray())
            seed = unchecked(seed * 31 + b);
        foreach (var b in questionId.ToByteArray())
            seed = unchecked(seed * 31 + b);
        return seed;
    }
}
