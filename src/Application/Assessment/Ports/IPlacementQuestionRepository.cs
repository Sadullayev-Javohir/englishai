using Domain.Assessment;

namespace Application.Assessment.Ports;

/// <summary>
/// Read access to the placement question bank. The implementation lives in the
/// Infrastructure layer (in-memory seed for Faza 0, EF Core later).
/// </summary>
public interface IPlacementQuestionRepository
{
    Task<PlacementQuestion?> GetByIdAsync(Guid questionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an available question for the given stage at (or nearest to) the
    /// requested difficulty, excluding questions already answered in the session.
    /// When several items sit at the same nearest difficulty, one is chosen
    /// pseudo-randomly using a seed derived from <paramref name="sessionId"/> and the
    /// number already excluded, so different learners get a different mix of items
    /// while a single session stays reproducible. Returns <c>null</c> only when the
    /// stage's bank is exhausted.
    /// </summary>
    Task<PlacementQuestion?> GetNextAsync(
        TestStage stage,
        CefrLevel difficulty,
        IReadOnlyCollection<Guid> excludeQuestionIds,
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
