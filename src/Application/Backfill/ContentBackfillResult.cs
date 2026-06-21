namespace Application.Backfill;

/// <summary>The outcome of a content backfill run, for logging and the admin response.</summary>
public sealed record ContentBackfillResult(int Requested, int Completed, int Filled);
