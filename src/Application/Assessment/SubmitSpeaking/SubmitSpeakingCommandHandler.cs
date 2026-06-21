using Application.Assessment.Common;
using Application.Assessment.Ports;
using Application.Common;
using Domain.Assessment;
using Domain.Common;
using MediatR;

namespace Application.Assessment.SubmitSpeaking;

public sealed class SubmitSpeakingCommandHandler
    : IRequestHandler<SubmitSpeakingCommand, SubmitSpeakingResult>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly IPlacementQuestionRepository _questions;
    private readonly IPlacementProductiveTaskProvider _tasks;
    private readonly IPlacementSpeakingAssessor _assessor;
    private readonly ICurrentUserAccessor? _currentUser;

    public SubmitSpeakingCommandHandler(
        IPlacementSessionStore sessions,
        IPlacementQuestionRepository questions,
        IPlacementProductiveTaskProvider tasks,
        IPlacementSpeakingAssessor assessor,
        ICurrentUserAccessor? currentUser = null)
    {
        _sessions = sessions;
        _questions = questions;
        _tasks = tasks;
        _assessor = assessor;
        _currentUser = currentUser;
    }

    public async Task<SubmitSpeakingResult> Handle(
        SubmitSpeakingCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken)
            ?? throw new PlacementSessionExpiredException(request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);
        if (session.IsIntegrityInvalidated)
            throw new PlacementIntegrityViolationException(session.Id);

        if (session.CurrentStage != TestStage.Speaking)
            throw new DomainException("The placement test is not on the Speaking stage.");

        var placementTask = _tasks.GetSpeakingTask(session.CurrentDifficulty);
        if (placementTask.Id != request.TaskId)
            throw new DomainException("The submitted task does not match the current Speaking task.");

        var assessment = await _assessor.AssessAsync(request.AudioContent, placementTask, cancellationToken);

        if (!assessment.ShouldAdvance)
        {
            return new SubmitSpeakingResult(
                0,
                CefrLevel.A1,
                assessment.Outcome,
                Retryable: true,
                IsTestCompleted: false,
                session.CurrentStage,
                session.CurrentDifficulty,
                NextItem: null);
        }

        var score = PlacementScoring.CapProductiveScore(assessment.Score, placementTask.Difficulty);
        session.RecordProductiveResult(request.TaskId, score, placementTask.Difficulty);

        var nextItem = await PlacementItemResolver.NextItemAsync(
            session, _questions, _tasks, cancellationToken);
        await _sessions.SaveAsync(session, cancellationToken);

        return new SubmitSpeakingResult(
            score,
            Domain.Assessment.CefrLevelExtensions.FromScore(score),
            assessment.Outcome,
            Retryable: false,
            session.IsCompleted,
            session.CurrentStage,
            session.CurrentDifficulty,
            nextItem);
    }
}
