using Infrastructure.Persistence;

namespace Web;

internal sealed record DatabaseCommand(bool ReconcileCatalog, TimeSpan LockTimeout)
{
    public static DatabaseCommand? Parse(string[] args)
    {
        if (args.Length == 0)
            return null;

        if (!args[0].Equals("database", StringComparison.OrdinalIgnoreCase))
            return null;

        if (args.Length < 2 || !args[1].Equals("migrate", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Supported command: database migrate [--no-reconcile] [--lock-timeout-seconds <seconds>].");

        var reconcileCatalog = true;
        var lockTimeout = TimeSpan.FromMinutes(5);
        for (var index = 2; index < args.Length; index++)
        {
            if (args[index].Equals("--no-reconcile", StringComparison.OrdinalIgnoreCase))
            {
                reconcileCatalog = false;
                continue;
            }

            if (args[index].Equals("--lock-timeout-seconds", StringComparison.OrdinalIgnoreCase) &&
                index + 1 < args.Length &&
                int.TryParse(args[++index], out var seconds) && seconds is >= 1 and <= 3600)
            {
                lockTimeout = TimeSpan.FromSeconds(seconds);
                continue;
            }

            throw new ArgumentException($"Unsupported database command option: {args[index]}.");
        }

        return new DatabaseCommand(reconcileCatalog, lockTimeout);
    }

    public static async Task<int> RunAsync(
        IServiceProvider services,
        DatabaseCommand command,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await DatabaseInitializer.InitializeAsync(
                services,
                new DatabaseInitializationOptions(
                    ReconcileCatalog: command.ReconcileCatalog,
                    LockTimeout: command.LockTimeout),
                cancellationToken);
            logger.LogInformation(
                "Database command completed. Pending migrations applied: {MigrationCount}; seeded: {Seeded}; reconciled: {Reconciled}.",
                result.PendingMigrations.Count,
                result.SeededCatalog,
                result.ReconciledCatalog);
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Database command failed.");
            return 1;
        }
    }
}
