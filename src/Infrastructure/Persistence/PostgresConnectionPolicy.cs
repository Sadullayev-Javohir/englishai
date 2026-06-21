using Npgsql;

namespace Infrastructure.Persistence;

public static class PostgresConnectionPolicy
{
    public const int GlobalMaxConnections = 200;
    public const int ClientConnectionBudget = 140;
    public const int PerHostServerConnectionBudget = 24;

    public static void ValidateApplication(string connectionString, PostgresOptions options)
    {
        if (options.PoolSize > options.ServerConnectionBudget)
            throw new InvalidOperationException("Postgres:PoolSize cannot exceed Postgres:ServerConnectionBudget.");

        if (options.ServerConnectionBudget > PerHostServerConnectionBudget)
            throw new InvalidOperationException($"Postgres:ServerConnectionBudget cannot exceed {PerHostServerConnectionBudget} per host.");

        if (!options.RequireTransactionPooling)
            return;

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Port != 6432)
            throw new InvalidOperationException("Production application traffic must use the PgBouncer transaction endpoint on port 6432.");
        if (builder.Pooling)
            throw new InvalidOperationException("Npgsql client pooling must be disabled behind PgBouncer transaction pooling.");
        if (builder.MaxAutoPrepare > 0)
            throw new InvalidOperationException("Prepared statement auto-prepare must be disabled behind PgBouncer transaction pooling.");
    }

    public static string ApplyPoolBudget(string connectionString, int poolSize)
    {
        if (poolSize is < 1 or > PerHostServerConnectionBudget)
            throw new InvalidOperationException($"PostgreSQL pool size must be between 1 and {PerHostServerConnectionBudget}.");

        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = poolSize,
        };
        return builder.ConnectionString;
    }

    public static void ValidateSession(string connectionString, string name)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Port != 6433)
            throw new InvalidOperationException($"{name} must use the PgBouncer session endpoint on port 6433.");
    }

    public static void ValidateMigration(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (builder.Port is 6432 or 6433)
            throw new InvalidOperationException("Database migrations must bypass PgBouncer and connect to the Patroni primary route.");
        var usesMultipleHosts = (builder.Host ?? string.Empty).Contains(',', StringComparison.Ordinal);
        if (!usesMultipleHosts && !string.IsNullOrEmpty(builder.TargetSessionAttributes))
            throw new InvalidOperationException("Single-host database migrations must not set Target Session Attributes.");
        if (usesMultipleHosts && builder.TargetSessionAttributes != "read-write")
            throw new InvalidOperationException("Database migrations require Target Session Attributes=read-write.");
    }
}
