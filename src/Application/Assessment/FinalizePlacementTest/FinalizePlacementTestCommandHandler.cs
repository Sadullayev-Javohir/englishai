using Application.Analytics.Ports;
using Application.Assessment.Dtos;
using Application.Assessment.Ports;
using Application.Common;
using Application.Learning.Ports;
using Domain.Analytics;
using Domain.Learning;
using MediatR;

namespace Application.Assessment.FinalizePlacementTest;

public sealed class FinalizePlacementTestCommandHandler
    : IRequestHandler<FinalizePlacementTestCommand, PlacementResultDto>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;

    public FinalizePlacementTestCommandHandler(
        IPlacementSessionStore sessions,
        ILearnerProfileRepository profiles,
        IProductEventStore productEvents,
        TimeProvider clock,
        ICurrentUserAccessor? currentUser = null)
    {
        _sessions = sessions;
        _profiles = profiles;
        _productEvents = productEvents;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<PlacementResultDto> Handle(
        FinalizePlacementTestCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken)
            ?? throw new PlacementSessionExpiredException(request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);
        if (session.IsIntegrityInvalidated)
            throw new PlacementIntegrityViolationException(session.Id);

        if (!session.IsCompleted)
            throw new Domain.Common.DomainException("The placement test must be completed before it can be finalized.");

        var result = session.Finalize();
        if (session.PlacementApplied) return PlacementResultDto.FromDomain(result);

        // Persist the result into the learner model (PROJECT-SPEC Faza 2): create a
        // new profile or re-seed the existing one if the learner retook the test.
        var now = _clock.GetUtcNow();
        var profile = await _profiles.GetByLearnerIdAsync(session.LearnerId, cancellationToken);
        if (profile is null)
            profile = LearnerProfile.CreateFromPlacement(session.LearnerId, result, now);
        else
            profile.ApplyPlacement(result, now);

        await _profiles.SaveAsync(profile, cancellationToken);
        await _productEvents.AppendOnceAsync(
            session.LearnerId,
            ProductEventType.PlacementCompleted,
            now,
            source: session.Id.ToString(),
            cancellationToken);
        // A retried/lost final response must not reset skill seeds after the learner
        // has begun studying. Only mark applied after persistence has succeeded.
        session.MarkPlacementApplied();
        await _sessions.SaveAsync(session, cancellationToken);

        return PlacementResultDto.FromDomain(result);
    }
}
