using Domain.Gamification;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Gamification;

public class DiscountCatalogTests
{
    [Fact]
    public void FindByCoinsCost_returns_the_matching_tier()
    {
        var tier = DiscountCatalog.FindByCoinsCost(8_000);

        tier.Should().NotBeNull();
        tier!.DiscountPercent.Should().Be(10);
    }

    [Fact]
    public void Catalog_scales_from_four_thousand_points_for_five_percent()
    {
        DiscountCatalog.Tiers.Should().Equal(
            new DiscountTier(4_000, 5),
            new DiscountTier(8_000, 10),
            new DiscountTier(16_000, 20),
            new DiscountTier(24_000, 30));
    }

    [Fact]
    public void FindByCoinsCost_returns_null_for_an_unknown_amount()
    {
        DiscountCatalog.FindByCoinsCost(999).Should().BeNull();
    }

    [Fact]
    public void Catalog_never_exceeds_the_revenue_safety_cap()
    {
        DiscountCatalog.Tiers.Should().OnlyContain(
            tier => tier.DiscountPercent <= DiscountCatalog.MaximumDiscountPercent);
    }

    [Theory]
    [InlineData(59_990, 5, 56_991)]
    [InlineData(59_990, 10, 53_991)]
    [InlineData(59_990, 20, 47_992)]
    [InlineData(59_990, 30, 41_993)]
    public void Discounted_amount_preserves_at_least_seventy_percent_revenue(
        int price, int discountPercent, int expectedAmount)
    {
        DiscountCatalog.CalculateDiscountedAmount(price, discountPercent).Should().Be(expectedAmount);
    }

    [Fact]
    public void Discount_above_the_safety_cap_is_rejected()
    {
        var act = () => DiscountCatalog.CalculateDiscountedAmount(59_990, 31);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
