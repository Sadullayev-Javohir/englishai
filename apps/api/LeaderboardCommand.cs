using Application.Gamification;

namespace Web;

internal sealed record LeaderboardCommand
{
    public static LeaderboardCommand? Parse(string[] args)
    {
        if (args.Length == 0 || !args[0].Equals("leaderboard", StringComparison.OrdinalIgnoreCase))
            return null;

        if (args.Length != 2 || !args[1].Equals("rebuild", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Supported command: leaderboard rebuild.");

        return new LeaderboardCommand();
    }

    public static async Task<int> RunAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var result = await scope.ServiceProvider
                .GetRequiredService<RebuildLeaderboardService>()
                .RebuildAsync(cancellationToken);
            logger.LogInformation(
                "Leaderboard rebuild completed. Durable ledgers: {LedgerCount}; indexed positive-XP learners: {IndexedCount}.",
                result.LedgerCount,
                result.IndexedCount);
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "Leaderboard rebuild failed.");
            return 1;
        }
    }
}
