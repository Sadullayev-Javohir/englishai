using Domain.Assessment;

namespace Application.Assessment.Ports;

/// <summary>
/// Supplies the curated free-response tasks for the placement test's productive
/// stages. The provider is deterministic for a given difficulty so a stage's task can
/// be re-resolved when the learner submits, without persisting the prompt server-side.
/// Content (English prompts) lives in the Infrastructure layer.
/// </summary>
public interface IPlacementProductiveTaskProvider
{
    /// <summary>The Writing task at (or nearest to) the requested difficulty.</summary>
    PlacementWritingTask GetWritingTask(CefrLevel difficulty);

    /// <summary>The Speaking task at (or nearest to) the requested difficulty.</summary>
    PlacementSpeakingTask GetSpeakingTask(CefrLevel difficulty);
}
