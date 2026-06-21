using Application.Ai;
using Application.Common;
using Application.Speaking.AccentTutors;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

/// <summary>
/// Meters realtime Voice Live sessions, which the server cannot observe directly: the browser
/// streams audio straight to Azure, so the only two moments the backend is involved are minting
/// the token and receiving the completion report.
///
/// The model is reserve-then-reconcile. Minting books a small up-front charge; completing replaces
/// it with the real duration. A session that never reports (crashed tab, closed laptop) simply
/// keeps its reservation — bounded loss, no sweeper job needed, because a dead tab also closes the
/// WebSocket and stops Azure billing.
///
/// Client-reported durations are never trusted: they are clamped against the mint timestamp and
/// against the configured per-session ceiling.
/// </summary>
public sealed class AzureVoiceLiveSessionMeter : IVoiceLiveSessionMeter
{
    /// <summary>Tolerance for browser/server clock skew when clamping a reported duration.</summary>
    private const double ClockSkewGraceSeconds = 5;

    /// <summary>How long past its ceiling a reservation stays settleable before Redis drops it.</summary>
    private static readonly TimeSpan SettlementGrace = TimeSpan.FromMinutes(5);

    private readonly AzureVoiceLiveOptions _options;
    private readonly IVariableCostMeter _costs;
    private readonly IVoiceLiveSessionStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<AzureVoiceLiveSessionMeter> _logger;

    public AzureVoiceLiveSessionMeter(
        AzureVoiceLiveOptions options,
        IVariableCostMeter costs,
        IVoiceLiveSessionStore store,
        TimeProvider clock,
        ILogger<AzureVoiceLiveSessionMeter> logger)
    {
        _options = options;
        _costs = costs;
        _store = store;
        _clock = clock;
        _logger = logger;
    }

    public async Task<VoiceLiveSessionTicket> ReserveAsync(
        string tutorId,
        CancellationToken cancellationToken = default)
    {
        var admission = AiAdmissionContext.Current;
        var now = _clock.GetUtcNow();
        var day = now.ToLocalDate();

        // 1. Global daily budget (sheds free accounts once the platform's spend is exhausted).
        _costs.EnsureAllowed(admission.Tier, AiFeature.SpeakingTutor, admission.CallerKey);

        // 2. Per-learner allowance. Checked before the reservation is written so a learner who is
        //    already at the cap is refused rather than pushed further over it.
        var reserved = _options.ReservationMinutes;
        var usedMinutes = await _store.GetDailyMinutesAsync(admission.CallerKey, day, cancellationToken);
        if (usedMinutes + reserved > _options.DailyMinutesPerLearner)
        {
            _logger.LogInformation(
                "Voice Live refused for {Caller}: {Used:F1} of {Limit} daily minutes already used.",
                admission.CallerKey,
                usedMinutes,
                _options.DailyMinutesPerLearner);
            throw new AiAdmissionException(
                "voice_live_daily_limit",
                "Daily live tutor allowance is used up for today.",
                SecondsUntilTomorrow(now),
                429);
        }

        // 3. Book the reservation. Store first: if the cost record fails we would rather have a
        //    settleable session than an unrefundable charge.
        var sessionId = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromMinutes(_options.MaxSessionMinutes) + SettlementGrace;
        await _store.CreateAsync(
            new VoiceLiveSessionRecord(sessionId, admission.CallerKey, tutorId, now, reserved),
            ttl,
            cancellationToken);

        var remaining = await _store.AddDailyMinutesAsync(admission.CallerKey, day, reserved, cancellationToken);
        RecordCost(reserved, admission, adjustment: false);

        return new VoiceLiveSessionTicket(
            sessionId,
            _options.MaxSessionMinutes * 60,
            Math.Max(0, _options.DailyMinutesPerLearner - remaining));
    }

    public async Task<VoiceLiveSessionSettlement> SettleAsync(
        string sessionId,
        double reportedSeconds,
        CancellationToken cancellationToken = default)
    {
        var admission = AiAdmissionContext.Current;
        var record = await _store.TakeAsync(sessionId, cancellationToken);

        // Unknown, expired or already-settled: report success. A 4xx here would teach the client
        // to stop reporting, and every unreported session costs the platform its full reservation.
        if (record is null)
            return new VoiceLiveSessionSettlement(0, await RemainingAsync(admission.CallerKey, cancellationToken), true);

        if (!string.Equals(record.CallerKey, admission.CallerKey, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Voice Live settlement for session {SessionId} came from {Caller} but was reserved by another caller; ignoring.",
                sessionId,
                admission.CallerKey);
            return new VoiceLiveSessionSettlement(0, await RemainingAsync(admission.CallerKey, cancellationToken), true);
        }

        var now = _clock.GetUtcNow();
        var billedSeconds = ClampReportedSeconds(reportedSeconds, record.StartedAt, now);
        var billedMinutes = billedSeconds / 60d;
        var deltaMinutes = billedMinutes - record.ReservedMinutes;

        var day = record.StartedAt.ToLocalDate();
        var remaining = await _store.AddDailyMinutesAsync(record.CallerKey, day, deltaMinutes, cancellationToken);
        RecordCost(deltaMinutes, admission, adjustment: true);

        return new VoiceLiveSessionSettlement(
            billedSeconds,
            Math.Max(0, _options.DailyMinutesPerLearner - remaining),
            false);
    }

    public async Task CancelAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var record = await _store.TakeAsync(sessionId, cancellationToken);
        if (record is null)
            return;

        var day = record.StartedAt.ToLocalDate();
        await _store.AddDailyMinutesAsync(record.CallerKey, day, -record.ReservedMinutes, cancellationToken);
        RecordCost(-record.ReservedMinutes, AiAdmissionContext.Current, adjustment: true);
    }

    /// <summary>
    /// A reported duration is capped by how long the server has actually held the reservation
    /// (plus clock-skew grace) and by the per-session ceiling, so neither an inflated client value
    /// nor a tab left open overnight can bill more than the configured maximum.
    /// </summary>
    internal static double ClampReportedSeconds(
        double reportedSeconds,
        DateTimeOffset startedAt,
        DateTimeOffset now,
        int maxSessionMinutes = 0)
    {
        var serverElapsed = Math.Max(0, (now - startedAt).TotalSeconds);
        var ceiling = serverElapsed + ClockSkewGraceSeconds;
        if (maxSessionMinutes > 0)
            ceiling = Math.Min(ceiling, maxSessionMinutes * 60d);

        // A client that reports nothing usable is billed the full ceiling, never zero.
        if (!double.IsFinite(reportedSeconds))
            return ceiling;

        return Math.Clamp(reportedSeconds, 0, ceiling);
    }

    private double ClampReportedSeconds(double reportedSeconds, DateTimeOffset startedAt, DateTimeOffset now) =>
        ClampReportedSeconds(reportedSeconds, startedAt, now, _options.MaxSessionMinutes);

    private void RecordCost(double minutes, AiAdmissionContext.State admission, bool adjustment)
    {
        var audioHours = minutes / 60d;
        var cost = audioHours * _options.EffectiveCostPerAudioHourUsd;

        if (adjustment)
        {
            _costs.RecordAdjustment(
                VariableCostCategory.VoiceLive,
                audioHours,
                "audio_hour",
                cost,
                admission.CallerKey,
                AiFeature.SpeakingTutor,
                admission.RequestPath);
            return;
        }

        _costs.Record(
            VariableCostCategory.VoiceLive,
            audioHours,
            "audio_hour",
            cost,
            admission.CallerKey,
            AiFeature.SpeakingTutor,
            admission.RequestPath);
    }

    private async Task<double> RemainingAsync(string callerKey, CancellationToken cancellationToken)
    {
        var day = _clock.GetUtcNow().ToLocalDate();
        var used = await _store.GetDailyMinutesAsync(callerKey, day, cancellationToken);
        return Math.Max(0, _options.DailyMinutesPerLearner - used);
    }

    private static int SecondsUntilTomorrow(DateTimeOffset now)
    {
        var tomorrow = now.UtcDateTime.Date.AddDays(1);
        return Math.Max(1, (int)(tomorrow - now.UtcDateTime).TotalSeconds);
    }
}
