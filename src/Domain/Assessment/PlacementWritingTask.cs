using Domain.Common;

namespace Domain.Assessment;

/// <summary>
/// A curated free-text writing prompt used in the placement test's Writing stage.
/// Unlike the old multiple-choice proxy, the learner actually writes a short response
/// which is scored by the writing assessor (the same four-dimension port used by the
/// Writing module). The prompt and its CEFR level + word range are content data
/// (English); all learner-facing Uzbek text comes from vetted templates (docs/development-guide.md
/// rule 11).
/// </summary>
public sealed class PlacementWritingTask
{
    private PlacementWritingTask(
        Guid id, CefrLevel difficulty, string prompt, int minWords, int maxWords)
    {
        Id = id;
        Difficulty = difficulty;
        Prompt = prompt;
        MinWords = minWords;
        MaxWords = maxWords;
    }

    public Guid Id { get; }
    public CefrLevel Difficulty { get; }

    /// <summary>The English writing prompt shown to the learner.</summary>
    public string Prompt { get; }

    /// <summary>The minimum expected word count for this task's level.</summary>
    public int MinWords { get; }

    /// <summary>The maximum expected word count for this task's level.</summary>
    public int MaxWords { get; }

    public static PlacementWritingTask Create(
        Guid id, CefrLevel difficulty, string prompt, int minWords, int maxWords)
    {
        if (id == Guid.Empty)
            throw new DomainException("Writing task id must not be empty.");
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Writing prompt must not be empty.");
        if (minWords < 1)
            throw new DomainException("Minimum word count must be at least 1.");
        if (maxWords < minWords)
            throw new DomainException("Maximum word count must be at least the minimum.");

        return new PlacementWritingTask(id, difficulty, prompt.Trim(), minWords, maxWords);
    }
}
