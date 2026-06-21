using System.Collections.Concurrent;
using Application.Subscription.Ports;

namespace Infrastructure.Subscription;

/// <summary>
/// Process-local <see cref="IMeteredAllowanceStore"/> for development and tests, where no Redis is
/// configured. Keeps the same clamp-at-zero contract so behaviour does not diverge from production.
/// </summary>
public sealed class InMemoryMeteredAllowanceStore : IMeteredAllowanceStore
{
    private readonly ConcurrentDictionary<string, double> _totals = new(StringComparer.Ordinal);

    public Task<double> AddAsync(
        string scope,
        string subjectKey,
        string periodKey,
        double delta,
        CancellationToken cancellationToken = default)
    {
        var total = _totals.AddOrUpdate(
            Key(scope, subjectKey, periodKey),
            _ => Math.Max(0, delta),
            (_, current) => Math.Max(0, current + delta));
        return Task.FromResult(total);
    }

    public Task<double> GetAsync(
        string scope,
        string subjectKey,
        string periodKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_totals.TryGetValue(Key(scope, subjectKey, periodKey), out var total)
            ? Math.Max(0, total)
            : 0);

    private static string Key(string scope, string subjectKey, string periodKey) =>
        $"{scope}:{periodKey}:{subjectKey}";
}
