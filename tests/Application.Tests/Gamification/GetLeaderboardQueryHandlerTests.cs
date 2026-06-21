using Application.Gamification.GetLeaderboard;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Subscription.Ports;
using Application.Tests.Learning;
using Domain.Assessment;
using Domain.Identity;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Gamification;

public class GetLeaderboardQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Viewer = Guid.NewGuid();

    private readonly ILeaderboardStore _leaderboard = Substitute.For<ILeaderboardStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();

    private GetLeaderboardQueryHandler Handler() =>
        new(_leaderboard, _profiles, _accounts, _subscriptions, new FixedTimeProvider(Now));

    private static UserAccount Account(string name) =>
        UserAccount.Register($"sub-{Guid.NewGuid():N}", $"{name}@example.com", name, null, Now);

    public GetLeaderboardQueryHandlerTests()
    {
        _subscriptions.GetManyByLearnerIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Domain.Subscription.Subscription>());
    }

    [Fact]
    public async Task Returns_the_top_entries_with_resolved_names_and_ranks()
    {
        var firstAccount = Account("Aziz");
        var secondAccount = Account("Dilnoza");
        _leaderboard.GetTopAsync(CefrLevel.B1, GetLeaderboardQueryHandler.TopSize, Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardRankEntry>
            {
                new(firstAccount.Id, 500, 1),
                new(secondAccount.Id, 300, 2),
            });
        _leaderboard.GetLearnerRankAsync(Viewer, CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns((LeaderboardRankEntry?)null);
        _accounts.GetManyByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserAccount> { firstAccount, secondAccount });

        var result = await Handler().Handle(new GetLeaderboardQuery(Viewer, CefrLevel.B1), CancellationToken.None);

        result.Level.Should().Be(CefrLevel.B1);
        result.Top.Should().HaveCount(2);
        result.Top[0].Rank.Should().Be(1);
        result.Top[0].DisplayName.Should().Be("Aziz");
        result.Top[0].Score.Should().Be(500);
        result.Top[1].Rank.Should().Be(2);
        result.CurrentUserEntry.Should().BeNull();
    }

    [Fact]
    public async Task Pins_the_viewers_own_row_when_they_rank_outside_the_top_list()
    {
        _leaderboard.GetTopAsync(CefrLevel.B1, GetLeaderboardQueryHandler.TopSize, Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardRankEntry>());
        _leaderboard.GetLearnerRankAsync(Viewer, CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new LeaderboardRankEntry(Viewer, 42, Rank: 100));

        var result = await Handler().Handle(new GetLeaderboardQuery(Viewer, CefrLevel.B1), CancellationToken.None);

        result.CurrentUserEntry.Should().NotBeNull();
        result.CurrentUserEntry!.Rank.Should().Be(100);
        result.CurrentUserEntry.IsCurrentUser.Should().BeTrue();
    }

    [Fact]
    public async Task Does_not_pin_a_separate_row_when_the_viewer_is_already_in_the_top_list()
    {
        _leaderboard.GetTopAsync(CefrLevel.B1, GetLeaderboardQueryHandler.TopSize, Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardRankEntry> { new(Viewer, 900, 1) });
        _leaderboard.GetLearnerRankAsync(Viewer, CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new LeaderboardRankEntry(Viewer, 900, Rank: 1));

        var result = await Handler().Handle(new GetLeaderboardQuery(Viewer, CefrLevel.B1), CancellationToken.None);

        result.CurrentUserEntry.Should().BeNull();
        result.Top.Single().IsCurrentUser.Should().BeTrue();
    }

    [Fact]
    public async Task Defaults_to_the_learners_own_current_level_when_none_is_requested()
    {
        var placement = new PlacementResult(CefrLevel.C1, CefrLevel.C1.ToScore(), new Dictionary<TestStage, StageResult>());
        var profile = LearnerProfile.CreateFromPlacement(Viewer, placement, Now);
        _profiles.GetByLearnerIdAsync(Viewer, Arg.Any<CancellationToken>()).Returns(profile);
        _leaderboard.GetTopAsync(CefrLevel.C1, GetLeaderboardQueryHandler.TopSize, Arg.Any<CancellationToken>())
            .Returns(new List<LeaderboardRankEntry>());
        _leaderboard.GetLearnerRankAsync(Viewer, CefrLevel.C1, Arg.Any<CancellationToken>())
            .Returns((LeaderboardRankEntry?)null);

        var result = await Handler().Handle(new GetLeaderboardQuery(Viewer, Level: null), CancellationToken.None);

        result.Level.Should().Be(CefrLevel.C1);
    }
}
