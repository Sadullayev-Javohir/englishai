using Domain.Common;
using Domain.Gamification;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Gamification;

public class DiscountRedemptionTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 8, 0, 0, TimeSpan.Zero);
    private static readonly DiscountTier Tier = DiscountCatalog.Tiers[0];

    [Fact]
    public void A_freshly_created_coupon_is_usable()
    {
        var redemption = DiscountRedemption.Create(Guid.NewGuid(), Tier, "ABCD1234", Now);

        redemption.IsUsable(Now).Should().BeTrue();
        redemption.ExpiresAt.Should().Be(Now.AddDays(DiscountRedemption.ExpiryDays));
    }

    [Fact]
    public void MarkUsed_consumes_the_coupon_and_it_is_no_longer_usable()
    {
        var redemption = DiscountRedemption.Create(Guid.NewGuid(), Tier, "ABCD1234", Now);

        redemption.MarkUsed(Now);

        redemption.IsUsable(Now).Should().BeFalse();
        redemption.UsedAt.Should().Be(Now);
    }

    [Fact]
    public void MarkUsed_twice_throws()
    {
        var redemption = DiscountRedemption.Create(Guid.NewGuid(), Tier, "ABCD1234", Now);
        redemption.MarkUsed(Now);

        var act = () => redemption.MarkUsed(Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void A_coupon_past_its_expiry_is_not_usable()
    {
        var redemption = DiscountRedemption.Create(Guid.NewGuid(), Tier, "ABCD1234", Now);
        var afterExpiry = Now.AddDays(DiscountRedemption.ExpiryDays + 1);

        redemption.IsUsable(afterExpiry).Should().BeFalse();
    }

    [Fact]
    public void ExpireIfElapsed_transitions_a_lapsed_unused_coupon_to_expired()
    {
        var redemption = DiscountRedemption.Create(Guid.NewGuid(), Tier, "ABCD1234", Now);
        var afterExpiry = Now.AddDays(DiscountRedemption.ExpiryDays + 1);

        redemption.ExpireIfElapsed(afterExpiry);

        redemption.Status.Should().Be(DiscountRedemptionStatus.Expired);
    }
}
