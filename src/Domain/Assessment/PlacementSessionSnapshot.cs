namespace Domain.Assessment;

/// <summary>
/// An immutable, serializable snapshot of a placement session's full state. The durable
/// session store (Redis) persists this between requests and across process restarts, then
/// rehydrates a live <see cref="PlacementTestSession"/> from it via
/// <see cref="PlacementTestSession.Restore"/>. It captures every field the adaptive engine
/// relies on - the stage sequence, the current position, the streak counters and all recorded
/// answers/results - so a resumed test behaves exactly as if it had never left memory.
/// </summary>
public sealed record PlacementSessionSnapshot(
    Guid Id,
    Guid LearnerId,
    IReadOnlyList<TestStage> StageSequence,
    TestStage CurrentStage,
    CefrLevel CurrentDifficulty,
    CefrLevel? StartingDifficulty,
    bool IsCompleted,
    int ConsecutiveCorrect,
    int ConsecutiveWrong,
    int QuestionsAnsweredInStage,
    IReadOnlyList<AnswerRecord> Answers,
    IReadOnlyList<ProductiveResult> ProductiveResults,
    Guid? CurrentItemId = null,
    CefrLevel? FinalizedLevel = null,
    int IntegrityViolationCount = 0,
    bool IsIntegrityInvalidated = false,
    IReadOnlyList<Guid>? IntegrityIncidentIds = null,
    bool PlacementApplied = false);
