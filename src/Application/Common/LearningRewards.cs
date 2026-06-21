namespace Application.Common;

/// <summary>
/// Runs a reward/gamification side effect so it cannot turn a successful learning result into a
/// failed request.
///
/// The daily goal, streak, XP and leaderboard updates that follow a submitted quiz live in Redis,
/// separate from the learner's durable progress in PostgreSQL. Before this existed, a Redis blip
/// meant a learner who had just answered a quiz correctly - and whose score was already committed -
/// received an HTTP 500 and a "try again" message, then re-submitted work that had already landed.
///
/// Losing a streak tick is a far smaller harm than losing (or double-counting) a learning result, so
/// the reward is explicitly best-effort. Call it ONLY after the core result is durable, and only for
/// effects that are idempotent on retry.
///
/// Cancellation is deliberately re-thrown: a caller who stopped listening is not a failure to
/// swallow, and swallowing it would keep work running for a request that no longer exists.
/// </summary>
public static class LearningRewards
{
    public static async Task AwardBestEffortAsync(
        Func<Task> award, CancellationToken cancellationToken)
    {
        try
        {
            await award();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Variant for a reward that returns a value the response wants to show (e.g. XP awarded).
    /// Returns <paramref name="fallback"/> when the reward could not be applied, so the learner sees
    /// their result with no reward rather than an error.
    /// </summary>
    public static async Task<T> AwardBestEffortAsync<T>(
        Func<Task<T>> award, T fallback, CancellationToken cancellationToken)
    {
        try
        {
            return await award();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
