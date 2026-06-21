namespace Application.Video.Ports;

public sealed record VideoExplainWork(
    string CacheKey,
    string VideoTitle,
    string Transcript,
    string FocusText,
    string UserMessage,
    IReadOnlyList<ChatTurn> History);

public sealed record VideoExplainSnapshot(
    int Active,
    int Queued,
    long Accepted,
    long Completed,
    long CacheHits,
    long Rejected,
    long Failed,
    long Unavailable,
    long CircuitOpenRejections,
    double AverageLatencyMs,
    double P95LatencyMs,
    bool CircuitOpen);

public interface IVideoExplainCoordinator
{
    Task<string?> ExecuteAsync(VideoExplainWork work, string callerKey, CancellationToken cancellationToken);
    VideoExplainSnapshot Snapshot();
}
