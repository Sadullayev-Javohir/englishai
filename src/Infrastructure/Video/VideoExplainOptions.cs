namespace Infrastructure.Video;

public sealed class VideoExplainOptions
{
    public const string SectionName = "VideoExplain";

    public int MaxConcurrency { get; set; } = 40;
    public int MaxQueueLength { get; set; } = 1000;
    public int PerUserRequestsPerMinute { get; set; } = 8;
    public int QueueTimeoutSeconds { get; set; } = 70;
    public int CircuitFailureThreshold { get; set; } = 8;
    public int CircuitBreakSeconds { get; set; } = 30;
}
