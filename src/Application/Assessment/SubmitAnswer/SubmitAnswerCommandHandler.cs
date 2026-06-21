using Application.Assessment.Common;
using Application.Assessment.Ports;
using Application.Common;
using Domain.Assessment;
using MediatR;

namespace Application.Assessment.SubmitAnswer;

public sealed class SubmitAnswerCommandHandler
    : IRequestHandler<SubmitAnswerCommand, SubmitAnswerResult>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly IPlacementQuestionRepository _questions;
    private readonly IPlacementProductiveTaskProvider _tasks;
    private readonly ICurrentUserAccessor? _currentUser;

    public SubmitAnswerCommandHandler(
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

    public async Task<SubmitAnswerResult> Handle(
        SubmitAnswerCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken)
            ?? throw new PlacementSessionExpiredException(request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);
        if (session.IsIntegrityInvalidated)
            throw new PlacementIntegrityViolationException(session.Id);

        var question = await _questions.GetByIdAsync(request.QuestionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Assessment.PlacementQuestion), request.QuestionId);

        if (question.Stage != session.CurrentStage)
            throw new Domain.Common.DomainException("The submitted question is not valid for the current placement stage.");

        // The learner's choice is an index into the shuffled options shown to them;
        // map it back to the original option index before grading.
        var originalIndex = OptionShuffle.ToOriginalIndex(
            session.Id, question.Id, question.Options.Count, request.SelectedOptionIndex);
        var wasCorrect = question.IsCorrect(originalIndex);
        session.RecordAnswer(question.Id, wasCorrect);

        var nextItem = await PlacementItemResolver.NextItemAsync(
            session, _questions, _tasks, cancellationToken);
        await _sessions.SaveAsync(session, cancellationToken);

        return new SubmitAnswerResult(
            wasCorrect,
            session.IsCompleted,
            session.CurrentStage,
            session.CurrentDifficulty,
            nextItem);
    }
}


