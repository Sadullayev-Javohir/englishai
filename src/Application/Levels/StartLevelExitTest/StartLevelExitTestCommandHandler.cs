using Application.Assessment.Common;
using Application.Assessment.Ports;
using Application.Common;
using Application.Identity.Dtos;
using Application.Learning.Ports;
using Domain.Assessment;
using Domain.Common;
using Domain.Learning;
using MediatR;

namespace Application.Levels.StartLevelExitTest;

public sealed class StartLevelExitTestCommandHandler
    : IRequestHandler<StartLevelExitTestCommand, StartLevelExitTestResult>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly IPlacementSessionStore _sessions;
    private readonly IPlacementQuestionRepository _questions;
    private readonly IPlacementProductiveTaskProvider _tasks;
    private readonly IAdminAuthorization? _admin;

    public StartLevelExitTestCommandHandler(
        ILearnerProfileRepository profiles,
        IPlacementSessionStore sessions,
        IPlacementQuestionRepository questions,
        IPlacementProductiveTaskProvider tasks,
        IAdminAuthorization? admin = null)
    {
        _profiles = profiles;
        _sessions = sessions;
        _questions = questions;
        _tasks = tasks;
        _admin = admin;
    }

    public async Task<StartLevelExitTestResult> Handle(
        StartLevelExitTestCommand request,
        CancellationToken cancellationToken)
    {
        // An exit test confirms readiness to leave the learner's *current* level, so a profile
        // (and therefore an established level) must already exist.
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), request.LearnerId);

        var testLevel = profile.OverallLevel;
        if (request.TestLevel is { } requestedLevel && requestedLevel != profile.OverallLevel)
        {
            var role = _admin is null
                ? AdminRole.None
                : await _admin.GetRoleAsync(request.LearnerId, cancellationToken);
            if (role != AdminRole.SuperAdmin)
                throw new ForbiddenException("Only the super-admin can start another level's exit test.");
            testLevel = requestedLevel;
        }

        if (testLevel >= CefrLevelExtensions.Ceiling)
            throw new DomainException("There is no exit test at the highest CEFR level - it is the final level.");

        // Reuse the adaptive placement engine (M.6) but pin the starting difficulty to the level
        // being left, so the questions probe at-level competence from the first item.
        var session = PlacementTestSession.Start(
            request.LearnerId, request.IncludeSpeaking, testLevel);

        var firstItem = await PlacementItemResolver.NextItemAsync(
            session, _questions, _tasks, cancellationToken);
        await _sessions.SaveAsync(session, cancellationToken);

        return new StartLevelExitTestResult(
            session.Id,
            testLevel,
            session.CurrentStage,
            LevelExitPolicy.RequirementsFor(testLevel),
            firstItem);
    }
}
