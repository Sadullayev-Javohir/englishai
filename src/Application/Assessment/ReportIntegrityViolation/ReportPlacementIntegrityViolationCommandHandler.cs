using Application.Assessment.Ports;
using Application.Common;
using MediatR;

namespace Application.Assessment.ReportIntegrityViolation;

public sealed class ReportPlacementIntegrityViolationCommandHandler
    : IRequestHandler<ReportPlacementIntegrityViolationCommand, ReportPlacementIntegrityViolationResult>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly ICurrentUserAccessor? _currentUser;

    public ReportPlacementIntegrityViolationCommandHandler(
        IPlacementSessionStore sessions,
        ICurrentUserAccessor? currentUser = null)
    {
        _sessions = sessions;
        _currentUser = currentUser;
    }

    public async Task<ReportPlacementIntegrityViolationResult> Handle(
        ReportPlacementIntegrityViolationCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken)
            ?? throw new PlacementSessionExpiredException(request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);

        session.RecordIntegrityViolation(request.IncidentId);
        await _sessions.SaveAsync(session, cancellationToken);

        // The stage and item number travel back to the endpoint purely so the incident can be
        // logged there: without them, a placement test killed by the integrity rules looks
        // exactly like a learner who walked away, which is how a mass onboarding failure
        // stayed invisible in production for weeks.
        return new ReportPlacementIntegrityViolationResult(
            session.IntegrityViolationCount,
            session.IsIntegrityInvalidated,
            session.CurrentStage.ToString(),
            session.CompletedItemCount + 1,
            session.TotalItemCount);
    }
}
