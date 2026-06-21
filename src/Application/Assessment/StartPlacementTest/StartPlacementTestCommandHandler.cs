using Application.Analytics.Ports;
using Application.Assessment.Common;
using Application.Assessment.Ports;
using Domain.Analytics;
using Domain.Assessment;
using MediatR;

namespace Application.Assessment.StartPlacementTest;

public sealed class StartPlacementTestCommandHandler
    : IRequestHandler<StartPlacementTestCommand, StartPlacementTestResult>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly IPlacementQuestionRepository _questions;
    private readonly IPlacementProductiveTaskProvider _tasks;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;

    public StartPlacementTestCommandHandler(
        IPlacementSessionStore sessions,
        IPlacementQuestionRepository questions,
        IPlacementProductiveTaskProvider tasks,
        IProductEventStore productEvents,
        TimeProvider clock)
    {
        _sessions = sessions;
        _questions = questions;
        _tasks = tasks;
        _productEvents = productEvents;
        _clock = clock;
    }

    public async Task<StartPlacementTestResult> Handle(
        StartPlacementTestCommand request,
        CancellationToken cancellationToken)
    {
        var session = PlacementTestSession.Start(request.LearnerId, request.IncludeSpeaking);
        await _productEvents.AppendOnceAsync(
            request.LearnerId,
            ProductEventType.PlacementStarted,
            _clock.GetUtcNow(),
            source: session.Id.ToString(),
            cancellationToken);

        var firstItem = await PlacementItemResolver.NextItemAsync(
            session, _questions, _tasks, cancellationToken);
        await _sessions.SaveAsync(session, cancellationToken);

        return new StartPlacementTestResult(session.Id, session.CurrentStage, firstItem);
    }
}
