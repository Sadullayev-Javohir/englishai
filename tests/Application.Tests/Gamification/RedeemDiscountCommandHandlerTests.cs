using Application.Gamification.Ports;
using Application.Gamification.RedeemDiscount;
using Application.Tests.Learning;
using Domain.Common;
using Domain.Gamification;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Gamification;

public class RedeemDiscountCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly ILearnerPointsRepository _points = Substitute.For<ILearnerPointsRepository>();
    private readonly IDiscountRedemptionRepository _redemptions = Substitute.For<IDiscountRedemptionRepository>();

    private RedeemDiscountCommandHandler Handler() => new(_points, _redemptions, new FixedTimeProvider(Now));

    [Fact]
    public async Task Redeeming_an_affordable_tier_debits_coins_and_issues_a_coupon()
    {
        var learnerPoints = LearnerPoints.CreateNew(Learner, Now);
        learnerPoints.Earn(9_000, Now);
        _points.GetOrCreateAsync(Learner, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(learnerPoints);
        _redemptions.GetByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DiscountRedemption?)null);

        var result = await Handler().Handle(new RedeemDiscountCommand(Learner, 8_000), CancellationToken.None);

        result.DiscountPercent.Should().Be(10);
        result.Code.Should().NotBeNullOrWhiteSpace();
        learnerPoints.SpendableCoins.Should().Be(1_000);
        await _points.Received(1).SaveAsync(learnerPoints, Arg.Any<CancellationToken>());
        await _redemptions.Received(1).SaveAsync(
            Arg.Is<DiscountRedemption>(r => r.LearnerId == Learner && r.DiscountPercent == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Redeeming_with_insufficient_coins_throws_and_saves_nothing()
    {
        var learnerPoints = LearnerPoints.CreateNew(Learner, Now);
        learnerPoints.Earn(100, Now);
        _points.GetOrCreateAsync(Learner, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(learnerPoints);

        var act = () => Handler().Handle(new RedeemDiscountCommand(Learner, 8_000), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _redemptions.DidNotReceive().SaveAsync(Arg.Any<DiscountRedemption>(), Arg.Any<CancellationToken>());
    }
}
