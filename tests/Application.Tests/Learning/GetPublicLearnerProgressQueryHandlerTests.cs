using Application.Analytics.Dtos;
using Application.Analytics.GetStudyStats;
using Application.Gamification.Dtos;
using Application.Gamification.GetGamificationStatus;
using Application.Gamification.GetPointsBalance;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Learning.Dtos;
using Application.Learning.GetGrowth;
using Application.Learning.GetLearnerOverview;
using Application.Learning.GetPublicLearnerProgress;
using Application.Levels.Dtos;
using Application.Levels.GetLevelMap;
using Application.Subscription.Ports;
using Domain.Assessment;
using Domain.Identity;
using Domain.Learning;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Xunit;

namespace Application.Tests.Learning;

public sealed class GetPublicLearnerProgressQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 8, 0, 0, TimeSpan.Zero);
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly ILeaderboardStore _leaderboard = Substitute.For<ILeaderboardStore>();

    [Fact]
    public async Task Returns_only_the_public_progress_projection()
    {
        var account = UserAccount.Register("public-progress-sub", "learner@example.com", "Dilnoza", null, Now);
        var skills = new List<SkillScoreDto> { new(SkillType.Speaking, 78, 12) };
        var overview = new LearnerOverviewDto(
            account.Id,
            CefrLevel.B1,
            skills,
            new List<ErrorHeatmapEntryDto> { new(ErrorCategory.Articles, 7) },
            new LevelStatusDto(false, 1, 4, false, false));
        var study = new StudyStatsDto(600, 3600, 9000, 12000, 18000, 12, 4, 1500, 2400,
            new List<DailyStudyBucketDto>(), new List<MonthlyStudyBucketDto>(),
            new List<SkillStudyBucketDto>(), new List<DailyStudyBucketDto>());
        var gamification = new GamificationStatusDto(3, 6, false, 4, 15, false, new List<string>());
        var growth = new List<GrowthPointDto> { new(Now, SkillType.Speaking, 78) };
        var levelMap = new LevelMapDto(
            CefrLevel.B1, CefrLevel.B1, true, true, false,
            new List<LevelCanDoDto>(), new List<Application.Vocabulary.Dtos.VocabularyTopicSummaryDto>(),
            20, 12, 8, null, null, new List<LevelSkillScoreDto>(), null, LevelExitState.Current);
        var points = new PointsBalanceDto(
            2450, 999, new List<DiscountTierDto>(), new List<RedemptionDto>
            {
                new("PRIVATE-CODE", 10, Now.AddDays(1)),
            });

        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _sender.Send(Arg.Any<GetLearnerOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(overview);
        _sender.Send(Arg.Any<GetStudyStatsQuery>(), Arg.Any<CancellationToken>()).Returns(study);
        _sender.Send(Arg.Any<GetGrowthQuery>(), Arg.Any<CancellationToken>()).Returns(growth);
        _sender.Send(Arg.Any<GetGamificationStatusQuery>(), Arg.Any<CancellationToken>()).Returns(gamification);
        _sender.Send(Arg.Any<GetLevelMapQuery>(), Arg.Any<CancellationToken>()).Returns(levelMap);
        _sender.Send(Arg.Any<GetPointsBalanceQuery>(), Arg.Any<CancellationToken>()).Returns(points);
        _leaderboard.GetLearnerRankAsync(account.Id, CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new LeaderboardRankEntry(account.Id, 2450, 3));

        var handler = new GetPublicLearnerProgressQueryHandler(
            _sender, _accounts, _subscriptions, _leaderboard, new FixedTimeProvider(Now));
        var result = await handler.Handle(
            new GetPublicLearnerProgressQuery(account.Id, new DateOnly(2026, 8, 14)),
            CancellationToken.None);

        result.DisplayName.Should().Be("Dilnoza");
        result.LifetimeXp.Should().Be(2450);
        result.Rank.Should().Be(3);
        result.SkillScores.Should().BeEquivalentTo(skills);
        result.TopicsLearned.Should().Be(12);
        result.GetType().GetProperties().Select(property => property.Name)
            .Should().NotContain(new[] { "ErrorHeatmap", "Recommendations", "SpendableCoins", "ActiveRedemptions" });
    }
}
