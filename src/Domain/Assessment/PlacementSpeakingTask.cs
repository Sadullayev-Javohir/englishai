using Domain.Common;

namespace Domain.Assessment;

/// <summary>
/// A curated speaking prompt used in the placement test's Speaking stage. The learner
/// records a short spoken response which is transcribed and scored (Azure Speech when a
/// key is configured; a clearly-marked offline estimate otherwise). The prompt is
/// content data (English); all learner-facing Uzbek text comes from vetted templates
/// (docs/development-guide.md rule 11).
/// </summary>
public sealed class PlacementSpeakingTask
{
    private PlacementSpeakingTask(
        Guid id, CefrLevel difficulty, string prompt, int minWords)
    {
        Id = id;
        Difficulty = difficulty;
        Prompt = prompt;
        MinWords = minWords;
    }

    public Guid Id { get; }
    public CefrLevel Difficulty { get; }

    /// <summary>The English speaking prompt (topic) shown to the learner.</summary>
    public string Prompt { get; }

    /// <summary>
    /// The number of spoken words a complete answer is expected to reach. Used by the
    /// assessor to gauge fluency/completeness of the transcribed response.
    /// </summary>
    public int MinWords { get; }

    public static PlacementSpeakingTask Create(
        Guid id, CefrLevel difficulty, string prompt, int minWords)
    {
        if (id == Guid.Empty)
            throw new DomainException("Speaking task id must not be empty.");
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Speaking prompt must not be empty.");
        if (minWords < 1)
            throw new DomainException("Minimum word count must be at least 1.");

        return new PlacementSpeakingTask(id, difficulty, prompt.Trim(), minWords);
    }
}
