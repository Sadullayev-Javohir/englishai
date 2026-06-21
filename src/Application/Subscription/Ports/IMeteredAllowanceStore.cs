namespace Application.Subscription.Ports;

/// <summary>
/// A per-period counter of a continuous quantity - minutes of speech, seconds of audio - as opposed
/// to <see cref="IUsageCounter"/>, which counts discrete uses and can only increment by one.
///
/// Speaking is billed by the second, so a daily speaking allowance cannot be expressed as a use
/// count: one learner's "session" is thirty seconds and another's is nine minutes. Amounts are
/// therefore <see cref="double"/> and the delta is signed, so a reservation can be reconciled
/// downward when the real duration turns out to be shorter than what was held.
/// </summary>
public interface IMeteredAllowanceStore
{
    /// <summary>
    /// Adds <paramref name="delta"/> (which may be negative) and returns the new total, never below
    /// zero. Implementations expire the counter shortly after its period ends, so callers do not
    /// have to clean up.
    /// </summary>
    Task<double> AddAsync(
        string scope,
        string subjectKey,
        string periodKey,
        double delta,
        CancellationToken cancellationToken = default);

    /// <summary>The amount consumed in this period so far; zero when nothing has been recorded.</summary>
    Task<double> GetAsync(
        string scope,
        string subjectKey,
        string periodKey,
        CancellationToken cancellationToken = default);
}

/// <summary>Named allowance scopes, so two features can never collide on one counter.</summary>
public static class MeteredAllowanceScope
{
    /// <summary>Minutes of learner speech sent to speech-to-text from an AI conversation.</summary>
    public const string SpeakingMinutes = "speaking-minutes";

    /// <summary>Minutes held or consumed by realtime Azure Voice Live sessions (accent tutors).</summary>
    public const string VoiceLiveMinutes = "voice-live-minutes";
}
