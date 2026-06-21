using MediatR;

namespace Application.Speaking.AccentTutors;

/// <summary>
/// Everything a browser needs to open a direct realtime Azure Voice Live session for one accent
/// tutor. The backend mints a short-lived token; the browser connects straight to Azure (no relay),
/// so the backend never sits in the realtime audio path.
/// </summary>
public sealed record VoiceLiveConnectionResult(
    string WebSocketUrl,               // wss://.../voice-live/realtime?...&agent-name=...  (no token)
    string AuthorizationQueryParameter, // query-string parameter the browser appends (e.g. "authorization")
    string AuthorizationValue,          // its value, e.g. "Bearer <token>"
    DateTimeOffset ExpiresAt,            // when the token stops working (browser must re-fetch)
    VoiceLiveSessionConfig Session,
    string SessionId = "",               // correlates the completion report back to this reservation
    int MaxSessionSeconds = 0);          // ceiling the server will bill, whatever the client reports

/// <summary>Client-side session.update parameters, resolved per tutor accent on the server.</summary>
public sealed record VoiceLiveSessionConfig(
    string VoiceName,
    string InputAudioFormat,
    string OutputAudioFormat,
    int InputSamplingRate,
    int SilenceDurationMs,
    string TurnDetectionType);

/// <summary>Port: mints a Voice Live connection (URL + short-lived token + per-accent session config).</summary>
public interface IVoiceLiveConnectionBroker
{
    bool IsConfigured { get; }

    Task<VoiceLiveConnectionResult> CreateAsync(string tutorId, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of reserving budget for a session that is about to start.</summary>
public sealed record VoiceLiveSessionTicket(
    string SessionId,
    int MaxSessionSeconds,
    double RemainingDailyMinutes);

/// <summary>Outcome of reconciling a finished session against its reservation.</summary>
public sealed record VoiceLiveSessionSettlement(
    double BilledSeconds,
    double RemainingDailyMinutes,
    bool AlreadySettled);

/// <summary>
/// Port: the only server-side view of a realtime session. The browser streams audio straight to
/// Azure, so cost is reserved when the token is minted and reconciled when the session reports
/// back. A session that never reports keeps its reservation — that is the intended failure mode.
/// </summary>
public interface IVoiceLiveSessionMeter
{
    /// <summary>Enforces the budget and the daily allowance, then books the up-front reservation.</summary>
    Task<VoiceLiveSessionTicket> ReserveAsync(string tutorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconciles a finished session. Idempotent: settling an unknown or already-settled session is
    /// a no-op, never an error, because rejecting a settle would strand the (larger) reservation.
    /// </summary>
    Task<VoiceLiveSessionSettlement> SettleAsync(
        string sessionId,
        double reportedSeconds,
        CancellationToken cancellationToken = default);

    /// <summary>Fully reverses a reservation whose session never started (e.g. Azure was down).</summary>
    Task CancelAsync(string sessionId, CancellationToken cancellationToken = default);
}

public sealed record GetAccentTutorVoiceLiveConnectionQuery(string TutorId)
    : IRequest<VoiceLiveConnectionResult>;

public sealed class GetAccentTutorVoiceLiveConnectionQueryHandler(
    IVoiceLiveConnectionBroker broker,
    IVoiceLiveSessionMeter meter)
    : IRequestHandler<GetAccentTutorVoiceLiveConnectionQuery, VoiceLiveConnectionResult>
{
    public async Task<VoiceLiveConnectionResult> Handle(
        GetAccentTutorVoiceLiveConnectionQuery request,
        CancellationToken cancellationToken)
    {
        AccentTutorStartQueryHandler.EnsureTutor(request.TutorId);
        if (!broker.IsConfigured)
            throw new SpeakingTutorUnavailableException("accent_tutor_not_configured", retryable: false);

        // Reserve before minting: the token is the only thing the browser cannot obtain on its own,
        // so this is the single point where a realtime session can still be refused.
        var ticket = await meter.ReserveAsync(request.TutorId, cancellationToken);
        try
        {
            var connection = await broker.CreateAsync(request.TutorId, cancellationToken);
            return connection with
            {
                SessionId = ticket.SessionId,
                MaxSessionSeconds = ticket.MaxSessionSeconds,
            };
        }
        catch
        {
            // Azure never handed out a socket, so nothing was spent. Give the learner their
            // allowance and the platform its budget back before the failure propagates.
            await meter.CancelAsync(ticket.SessionId, CancellationToken.None);
            throw;
        }
    }
}
