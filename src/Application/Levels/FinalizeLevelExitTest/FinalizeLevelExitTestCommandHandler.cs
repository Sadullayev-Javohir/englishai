using Application.Assessment;
using Application.Assessment.Dtos;
using Application.Assessment.Ports;
using Application.Common;
using Application.Gamification.Ports;
using Application.Learning.Ports;
using Domain.Assessment;
using Domain.Learning;
using MediatR;

namespace Application.Levels.FinalizeLevelExitTest;

public sealed class FinalizeLevelExitTestCommandHandler
    : IRequestHandler<FinalizeLevelExitTestCommand, FinalizeLevelExitTestResult>
{
    private readonly IPlacementSessionStore _sessions;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ILeaderboardStore _leaderboard;
    private readonly ILearnerPointsRepository _points;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;
    private readonly IAdminAuthorization? _admin;

    public FinalizeLevelExitTestCommandHandler(
        IPlacementSessionStore sessions,
        ILearnerProfileRepository profiles,
        ILeaderboardStore leaderboard,
        ILearnerPointsRepository points,
        TimeProvider clock,
        ICurrentUserAccessor? currentUser = null,
        IAdminAuthorization? admin = null)
    {
        _sessions = sessions;
        _profiles = profiles;
        _leaderboard = leaderboard;
        _points = points;
        _clock = clock;
        _currentUser = currentUser;
        _admin = admin;
    }

    public async Task<FinalizeLevelExitTestResult> Handle(
        FinalizeLevelExitTestCommand request,
        CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Assessment.PlacementTestSession), request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);
        if (session.IsIntegrityInvalidated)
            throw new PlacementIntegrityViolationException(session.Id);

        if (!session.IsCompleted)
            session.CompleteWithoutOptionalSpeaking();

        var result = session.Finalize();

        var profile = await _profiles.GetByLearnerIdAsync(session.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), session.LearnerId);

        var now = _clock.GetUtcNow();
        var testedLevel = session.StartingDifficulty;
        var previewRole = _admin is null
            ? Application.Identity.Dtos.AdminRole.None
            : await _admin.GetRoleAsync(session.LearnerId, cancellationToken);
        var isSuperAdminPreview = testedLevel != profile.OverallLevel &&
            previewRole == Application.Identity.Dtos.AdminRole.SuperAdmin;

        var exitEvaluation = LevelExitPolicy.Evaluate(testedLevel, result);
        var passed = exitEvaluation.Passed;
        var alreadyFinalized = session.FinalizedLevel is not null;
        var advanced = alreadyFinalized && session.FinalizedLevel > testedLevel;
        if (!isSuperAdminPreview && !alreadyFinalized)
        {
            profile.RecordConfirmationTest(passed, now);
            advanced = passed && profile.TryAdvanceLevel(now);
        }

        if (!isSuperAdminPreview && !alreadyFinalized)
            await _profiles.SaveAsync(profile, cancellationToken);

        if (!alreadyFinalized)
        {
            session.MarkExitTestFinalized(profile.OverallLevel);
            await _sessions.SaveAsync(session, cancellationToken);
        }

        if (advanced)
        {
            // Move the learner's leaderboard entry to their new CEFR level's cohort so they
            // don't linger on the board for the level they just left.
            var points = await _points.GetOrCreateAsync(session.LearnerId, now, cancellationToken);
            await _leaderboard.RemoveAsync(session.LearnerId, testedLevel, cancellationToken);
            await _leaderboard.SetScoreAsync(
                session.LearnerId,
                session.FinalizedLevel ?? profile.OverallLevel,
                points.LifetimeXp,
                cancellationToken);
        }

        var finalizedLevel = session.FinalizedLevel ?? profile.OverallLevel;
        var status = profile.LevelStatus(now);
        return new FinalizeLevelExitTestResult(
            passed,
            advanced,
            testedLevel,
            finalizedLevel,
            status.MasteredSkillCount,
            status.RequiredMasteredSkills,
            exitEvaluation.MinimumOverallScore,
            exitEvaluation.MinimumStageScore,
            exitEvaluation.ProductiveStageFloor,
            exitEvaluation.FailedStages,
            PlacementResultDto.FromDomain(result));
    }
}
