using MediatR;

namespace Application.Speaking.AccentTutors;

/// <summary>
/// Reports that a realtime Voice Live session has ended so the server can bill the real duration
/// instead of leaving the up-front reservation standing. <paramref name="DurationSeconds"/> is
/// client-reported and therefore untrusted — the meter clamps it against the mint timestamp.
/// </summary>
public sealed record CompleteAccentTutorVoiceLiveSessionCommand(
    string TutorId,
    string SessionId,
    double DurationSeconds) : IRequest<VoiceLiveSessionSettlement>;

public sealed class CompleteAccentTutorVoiceLiveSessionCommandHandler(IVoiceLiveSessionMeter meter)
    : IRequestHandler<CompleteAccentTutorVoiceLiveSessionCommand, VoiceLiveSessionSettlement>
{
    public Task<VoiceLiveSessionSettlement> Handle(
        CompleteAccentTutorVoiceLiveSessionCommand request,
        CancellationToken cancellationToken)
    {
        AccentTutorStartQueryHandler.EnsureTutor(request.TutorId);
        return meter.SettleAsync(request.SessionId, request.DurationSeconds, cancellationToken);
    }
}
