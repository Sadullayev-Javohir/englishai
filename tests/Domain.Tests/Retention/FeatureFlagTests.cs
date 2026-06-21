using Domain.Common;
using Domain.Retention;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Retention;

/// <summary>
/// Unit tests for feature-flag variant assignment (PROJECT-SPEC I.3): deterministic per
/// learner, disabled flags resolve to control, and weights split the cohort.
/// </summary>
public class FeatureFlagTests
{
    private static FeatureFlag TwoArmFlag(bool enabled = true) => new(
        "daily_goal_size",
        "3 vs 5 tasks",
        enabled,
        new[]
        {
            new FeatureVariant("control", 1, "3"),
            new FeatureVariant("variant_five", 1, "5")
        });

    [Fact]
    public void Assignment_is_stable_for_the_same_learner()
    {
        var flag = TwoArmFlag();
        var learner = Guid.NewGuid();

        var first = flag.VariantFor(learner);
        var second = flag.VariantFor(learner);

        second.Name.Should().Be(first.Name);
    }

    [Fact]
    public void A_disabled_flag_always_resolves_to_the_control_variant()
    {
        var flag = TwoArmFlag(enabled: false);

        for (var i = 0; i < 50; i++)
            flag.VariantFor(Guid.NewGuid()).Name.Should().Be("control");
    }

    [Fact]
    public void Both_variants_are_used_across_many_learners()
    {
        var flag = TwoArmFlag();

        var names = Enumerable.Range(0, 500)
            .Select(_ => flag.VariantFor(Guid.NewGuid()).Name)
            .Distinct()
            .ToList();

        names.Should().Contain(new[] { "control", "variant_five" });
    }

    [Fact]
    public void A_flag_requires_at_least_one_variant()
    {
        var act = () => new FeatureFlag("k", "d", true, Array.Empty<FeatureVariant>());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void A_variant_weight_must_be_positive()
    {
        var act = () => new FeatureVariant("control", 0);
        act.Should().Throw<DomainException>();
    }
}
