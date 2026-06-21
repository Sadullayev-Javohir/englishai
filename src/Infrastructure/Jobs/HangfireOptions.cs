namespace Infrastructure.Jobs;

public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";

    public bool Enabled { get; init; }
    public bool UseInMemoryStorage { get; init; }
    public string Queues { get; init; } = HangfireQueues.Notifications;
    public int WorkerCount { get; init; } = 4;
    public int DatabaseConnectionBudget { get; init; } = 16;
    public int RetryAttempts { get; init; } = 5;
    public string RetryDelaysSeconds { get; init; } = "15,60,300,900,3600";

    public string[] ParseQueues()
    {
        var queues = Queues.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(queue => queue.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (queues.Length == 0 || queues.Any(queue => !HangfireQueues.All.Contains(queue)))
            throw new InvalidOperationException($"Hangfire:Queues must contain only: {string.Join(", ", HangfireQueues.All)}.");

        return queues;
    }

    public void ValidateWorkerBudget(IReadOnlyCollection<string> queues)
    {
        var workerCount = Math.Clamp(WorkerCount, 1, 64);
        var databaseBudget = Math.Clamp(DatabaseConnectionBudget, 1, 1024);
        if (workerCount > databaseBudget)
            throw new InvalidOperationException("Hangfire worker count exceeds its database connection budget.");

        if (queues.Contains(HangfireQueues.Critical) && queues.Count > 1)
            throw new InvalidOperationException("The critical queue must run in a dedicated worker host.");
    }

    public int[] ParseRetryDelays()
    {
        var delays = RetryDelaysSeconds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var seconds) && seconds > 0
                ? seconds
                : throw new InvalidOperationException("Hangfire:RetryDelaysSeconds must contain positive integers."))
            .ToArray();

        if (delays.Length != Math.Clamp(RetryAttempts, 1, 10))
            throw new InvalidOperationException("Hangfire retry delay count must equal Hangfire:RetryAttempts.");

        return delays;
    }
}
