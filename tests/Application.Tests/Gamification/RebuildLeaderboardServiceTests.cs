using Application.Gamification;
using Application.Gamification.Ports;
using Application.Learning.Ports;
using Domain.Assessment;
using Domain.Gamification;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Gamification;

public sealed class RebuildLeaderboardServiceTests
{
    [Fact]
    public async Task Rebuilds_positive_xp_ledgers_at_their_current_level()
    {
        var now = DateTimeOffset.UtcNow;
        var a1Learner = Guid.NewGuid();
        var c1Learner = Guid.NewGuid();
        var zeroXpLearner = Guid.NewGuid();
        var points = Substitute.For<ILearnerPointsRepository>();
        var profiles = Substitute.For<ILearnerProfileRepository>();
        var leaderboard = Substitute.For<ILeaderboardStore>();
        points.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            Points(a1Learner, 25, now),
            Points(c1Learner, 80, now),
            LearnerPoints.CreateNew(zeroXpLearner, now),
        });
        profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            LearnerProfile.CreateAtLevel(a1Learner, CefrLevel.A1, now),
            LearnerProfile.CreateAtLevel(c1Learner, CefrLevel.C1, now),
        });

        var result = await new RebuildLeaderboardService(points, profiles, leaderboard)
            .RebuildAsync(CancellationToken.None);

        result.Should().Be(new LeaderboardRebuildResult(3, 2));
        await leaderboard.Received(1).ReplaceAllAsync(
            Arg.Is<IReadOnlyDictionary<CefrLevel, IReadOnlyDictionary<Guid, long>>>(scores =>
                scores[CefrLevel.A1].Count == 1 && scores[CefrLevel.A1][a1Learner] == 25 &&
                scores[CefrLevel.C1].Count == 1 && scores[CefrLevel.C1][c1Learner] == 80 &&
                scores.Values.Sum(level => level.Count) == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Defaults_a_learner_without_a_profile_to_A2()
    {
        var learnerId = Guid.NewGuid();
        var points = Substitute.For<ILearnerPointsRepository>();
        var profiles = Substitute.For<ILearnerProfileRepository>();
        var leaderboard = Substitute.For<ILeaderboardStore>();
        points.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { Points(learnerId, 40, DateTimeOffset.UtcNow) });
        profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<LearnerProfile>());

        await new RebuildLeaderboardService(points, profiles, leaderboard)
            .RebuildAsync(CancellationToken.None);

        await leaderboard.Received(1).ReplaceAllAsync(
            Arg.Is<IReadOnlyDictionary<CefrLevel, IReadOnlyDictionary<Guid, long>>>(scores =>
                scores[CefrLevel.A2].Count == 1 && scores[CefrLevel.A2][learnerId] == 40),
            Arg.Any<CancellationToken>());
    }

    private static LearnerPoints Points(Guid learnerId, int amount, DateTimeOffset now)
    {
        var ledger = LearnerPoints.CreateNew(learnerId, now);
        ledger.Earn(amount, now);
        return ledger;
    }
}
