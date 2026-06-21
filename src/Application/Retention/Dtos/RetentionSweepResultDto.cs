namespace Application.Retention.Dtos;

/// <summary>
/// Outcome of the daily retention sweep (PROJECT-SPEC I.2): how many learners were scanned
/// and how many win-back messages were dispatched this run.
/// </summary>
public sealed record RetentionSweepResultDto(int ScannedLearners, int WinBackMessagesSent);
