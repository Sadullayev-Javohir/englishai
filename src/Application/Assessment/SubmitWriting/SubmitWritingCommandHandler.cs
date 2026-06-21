using Application.Assessment.Common;
using Application.Assessment.Ports;
using Application.Common;
using Application.Writing.Ports;
using Domain.Assessment;
using Domain.Common;
using Domain.Writing;
using MediatR;

namespace Application.Assessment.SubmitWriting;

public sealed class SubmitWritingCommandHandler
    : IRequestHandler<SubmitWritingCommand, SubmitWritingResult>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly IPlacementQuestionRepository _questions;
    private readonly IPlacementProductiveTaskProvider _tasks;
    private readonly IWritingAssessor _assessor;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;

    public SubmitWritingCommandHandler(
        IPlacementSessionStore sessions,
        IPlacementQuestionRepository questions,
        IPlacementProductiveTaskProvider tasks,
        IWritingAssessor assessor,
        TimeProvider clock,
        ICurrentUserAccessor? currentUser = null)
    {
        _sessions = sessions;
        _questions = questions;
        _tasks = tasks;
        _assessor = assessor;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<SubmitWritingResult> Handle(
        SubmitWritingCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken)
            ?? throw new PlacementSessionExpiredException(request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);
        if (session.IsIntegrityInvalidated)
            throw new PlacementIntegrityViolationException(session.Id);

        if (session.CurrentStage != TestStage.Writing)
            throw new DomainException("The placement test is not on the Writing stage.");

        // Re-resolve the same curated task for the session's difficulty, then score the
        // submission across the four G.3 dimensions via the assessor port (rule 11).
        var placementTask = _tasks.GetWritingTask(session.CurrentDifficulty);
        if (placementTask.Id != request.TaskId)
            throw new DomainException("The submitted task does not match the current Writing task.");

        var wordCount = WritingTask.CountWords(request.Text);
        if (wordCount < placementTask.MinWords)
            throw new DomainException($"The Writing response must contain at least {placementTask.MinWords} words.");
        if (wordCount > placementTask.MaxWords)
            throw new DomainException($"The Writing response must contain at most {placementTask.MaxWords} words.");

        var writingTask = WritingTask.Create(
            placementTask.Prompt,
            placementTask.Difficulty,
            placementTask.MinWords,
            placementTask.MaxWords,
            _clock.GetUtcNow());

        var assessment = await _assessor.AssessAsync(
            writingTask, request.Text, placementTask.Difficulty, cancellationToken);
        var score = PlacementScoring.CapProductiveScore(
            assessment.OverallPercent, placementTask.Difficulty);

        session.RecordProductiveResult(request.TaskId, score, placementTask.Difficulty);

        var nextItem = await PlacementItemResolver.NextItemAsync(
            session, _questions, _tasks, cancellationToken);
        await _sessions.SaveAsync(session, cancellationToken);

        return new SubmitWritingResult(
            score,
            CefrLevelExtensions.FromScore(score),
            session.IsCompleted,
            session.CurrentStage,
            session.CurrentDifficulty,
            nextItem);
    }
}
