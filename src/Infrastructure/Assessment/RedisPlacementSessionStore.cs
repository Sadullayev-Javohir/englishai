using System.Text.Json;
using System.Runtime.CompilerServices;
using Application.Assessment.Ports;
using Application.Common;
using Domain.Assessment;
using Infrastructure.Redis;
using StackExchange.Redis;

namespace Infrastructure.Assessment;

/// <summary>
/// Durable <see cref="IPlacementSessionStore"/> backed by Redis. An in-flight placement test
/// spans ~25 requests over several minutes, so an in-memory store loses every active session
/// whenever the process restarts (a redeploy, crash or scale event) - the learner then hits a
/// generic error on their next answer or on finalize. Persisting each session as a JSON
/// snapshot keyed by its id makes the test survive restarts and lets multiple instances share
/// it. Wired only when a Redis connection is configured; otherwise the in-memory adapter is used.
/// </summary>
public sealed class RedisPlacementSessionStore : IPlacementSessionStore
{
    // A placement test is short-lived; keep a session alive for a few hours so a slow learner
    // (or a brief pause) never loses progress, then let Redis reclaim it automatically.
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(3);

    private readonly IRedisConnectionProvider _redis;
    private readonly ConditionalWeakTable<PlacementTestSession, ReadVersion> _versions = new();
    private sealed record ReadVersion(string Json);

    public RedisPlacementSessionStore(IRedisConnectionProvider redis)
    {
        _redis = redis;
    }

    private IDatabase Db => _redis.Get(RedisWorkload.Critical).GetDatabase();

    private string Key(Guid sessionId) => _redis.Key(RedisWorkload.Critical, "placement", sessionId.ToString("N"));

    public async Task<PlacementTestSession?> GetAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var value = await Db.StringGetAsync(Key(sessionId));
        if (!value.HasValue)
            return null;

        var snapshot = JsonSerializer.Deserialize<PlacementSessionSnapshot>(value!);
        if (snapshot is null) return null;
        var session = PlacementTestSession.Restore(snapshot);
        _versions.Add(session, new ReadVersion(value.ToString()));
        return session;
    }

    public async Task SaveAsync(PlacementTestSession session, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(session.ToSnapshot());
        cancellationToken.ThrowIfCancellationRequested();
        // A slow assessment/resume must not overwrite another answer or an integrity
        // incident saved while it was running. Compare the exact snapshot that was read.
        var transaction = Db.CreateTransaction();
        transaction.AddCondition(_versions.TryGetValue(session, out var version)
            ? Condition.StringEqual(Key(session.Id), version.Json)
            : Condition.KeyNotExists(Key(session.Id)));
        var save = transaction.StringSetAsync(Key(session.Id), json, SessionTtl);
        if (!await transaction.ExecuteAsync())
            throw new ConflictException("The placement session changed. Resume it before submitting again.");
        await save;
        _versions.Remove(session);
        _versions.Add(session, new ReadVersion(json));
    }
}
