using Application.Assessment.Common;
using Application.Assessment.Ports;
using Application.Common;
using MediatR;

namespace Application.Assessment.ResumePlacementTest;

public sealed class ResumePlacementTestQueryHandler
    : IRequestHandler<ResumePlacementTestQuery, ResumePlacementTestResult>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly IPlacementQuestionRepository _questions;
    private readonly IPlacementProductiveTaskProvider _tasks;
    private readonly ICurrentUserAccessor? _currentUser;

    public ResumePlacementTestQueryHandler(
        IPlacementSessionStore sessions,
        IPlacementQuestionRepository questions,
        IPlacementProductiveTaskProvider tasks,
        ICurrentUserAccessor? currentUser = null)
    {
        _sessions = sessions;
        _questions = questions;
        _tasks = tasks;
        _currentUser = currentUser;
    }

    public async Task<ResumePlacementTestResult> Handle(
        ResumePlacementTestQuery request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken)
            ?? throw new PlacementSessionExpiredException(request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);
        if (session.IsIntegrityInvalidated)
            throw new PlacementIntegrityViolationException(session.Id);

        var neededItem = !session.IsCompleted && session.CurrentItemId is null;
        var currentItem = await PlacementItemResolver.NextItemAsync(
            session, _questions, _tasks, cancellationToken);
        // Normal resume is a read, not a competing write against an in-flight answer.
        if (neededItem) await _sessions.SaveAsync(session, cancellationToken);

        return new ResumePlacementTestResult(session.Id, session.IsCompleted, currentItem);
    }
}
