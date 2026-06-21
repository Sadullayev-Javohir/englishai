using Domain.Assessment;

namespace Application.Assessment.Ports;

public enum PlacementSpeakingOutcome
{
    Scored,
    OffTopic,
    InvalidAudio,
    NoSpeech,
    LowConfidence,
    ServiceUnavailable,
}

/// <summary>The graded outcome of a placement speaking task.</summary>
public sealed record PlacementSpeakingScore(int Score, PlacementSpeakingOutcome Outcome)
{
    public bool ShouldAdvance =>
        Outcome is PlacementSpeakingOutcome.Scored or PlacementSpeakingOutcome.OffTopic;
}

/// <summary>
/// Scores a learner's recorded answer for a placement speaking task. The real adapter
/// uses Azure Speech (STT + pronunciation) when configured. Provider or audio failures
/// are returned as typed retryable outcomes and are never converted into fabricated scores.
/// All AI/Speech calls go through this port so the provider is swappable.
/// </summary>
public interface IPlacementSpeakingAssessor
{
    Task<PlacementSpeakingScore> AssessAsync(
        byte[] audioContent, PlacementSpeakingTask task, CancellationToken cancellationToken = default);
}
