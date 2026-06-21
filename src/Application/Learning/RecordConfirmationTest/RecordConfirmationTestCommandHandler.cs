using Application.Common;
using Application.Gamification.Ports;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Learning.RecordConfirmationTest;

public sealed class RecordConfirmationTestCommandHandler
    : IRequestHandler<RecordConfirmationTestCommand, ConfirmationTestResultDto>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly ILeaderboardStore _leaderboard;
    private readonly ILearnerPointsRepository _points;
    private readonly TimeProvider _clock;

    public RecordConfirmationTestCommandHandler(
        ILearnerProfileRepository profiles,
        ILeaderboardStore leaderboard,
        ILearnerPointsRepository points,
        TimeProvider clock)
    {
        _profiles = profiles;
        _leaderboard = leaderboard;
        _points = points;
        _clock = clock;
    }

    public async Task<ConfirmationTestResultDto> Handle(
        RecordConfirmationTestCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), request.LearnerId);

        var now = _clock.GetUtcNow();
        var levelBeforeAdvance = profile.OverallLevel;
        profile.RecordConfirmationTest(request.Passed, now);
        var advanced = profile.TryAdvanceLevel(now);

        await _profiles.SaveAsync(profile, cancellationToken);

        if (advanced)
        {
            // Move the learner's leaderboard entry to their new CEFR level's cohort so they
            // don't linger on the board for the level they just left.
            var points = await _points.GetOrCreateAsync(request.LearnerId, now, cancellationToken);
            await _leaderboard.RemoveAsync(request.LearnerId, levelBeforeAdvance, cancellationToken);
            await _leaderboard.SetScoreAsync(request.LearnerId, profile.OverallLevel, points.LifetimeXp, cancellationToken);
        }

        return new ConfirmationTestResultDto(profile.OverallLevel, advanced);
    }
}
