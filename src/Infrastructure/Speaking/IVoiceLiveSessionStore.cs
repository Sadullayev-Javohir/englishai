namespace Infrastructure.Speaking;

/// <summary>What the server knows about a realtime session between minting and settlement.</summary>
public sealed record VoiceLiveSessionRecord(
    string SessionId,
    string CallerKey,
    string TutorId,
    DateTimeOffset StartedAt,
    double ReservedMinutes);

/// <summary>
/// Holds in-flight Voice Live reservations and each learner's daily minute usage. Infrastructure
/// internal: the Application layer only ever sees <c>IVoiceLiveSessionMeter</c>.
/// </summary>
public interface IVoiceLiveSessionStore
{
    Task CreateAsync(VoiceLiveSessionRecord record, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically reads and removes the record. Atomicity is what makes settlement single-use: a
    /// replayed completion report finds nothing and cannot refund the reservation twice.
    /// </summary>
    Task<VoiceLiveSessionRecord?> TakeAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>Signed; a negative delta reconciles an over-reservation. Never returns below zero.</summary>
    Task<double> AddDailyMinutesAsync(
        string callerKey,
        DateOnly day,
        double deltaMinutes,
        CancellationToken cancellationToken);

    Task<double> GetDailyMinutesAsync(string callerKey, DateOnly day, CancellationToken cancellationToken);
}
