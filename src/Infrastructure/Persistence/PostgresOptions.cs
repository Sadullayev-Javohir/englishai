namespace Infrastructure.Persistence;

public sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    public int CommandTimeoutSeconds { get; init; } = 30;
    public int MaxRetryCount { get; init; }
    public int MaxRetryDelaySeconds { get; init; } = 2;
    public int PoolSize { get; init; } = 128;
    public int ServerConnectionBudget { get; init; } = 24;
    public bool RequireTransactionPooling { get; init; }
}
