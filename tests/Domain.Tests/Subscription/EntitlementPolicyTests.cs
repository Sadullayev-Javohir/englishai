using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Subscription;

/// <summary>Unit tests for the freemium gating policy (PROJECT-SPEC H.1).</summary>
public class EntitlementPolicyTests
{
    [Theory]
    [InlineData(PremiumFeature.SpeakingSession, 4, true)]
    [InlineData(PremiumFeature.SpeakingSession, 5, false)]
    [InlineData(PremiumFeature.NewVocabularyWord, 9, true)]
    [InlineData(PremiumFeature.NewVocabularyWord, 10, false)]
    [InlineData(PremiumFeature.WritingAssessment, 0, true)]
    [InlineData(PremiumFeature.WritingAssessment, 1, false)]
    [InlineData(PremiumFeature.AssistantQuestion, 9, true)]
    [InlineData(PremiumFeature.AssistantQuestion, 10, false)]
    [InlineData(PremiumFeature.PronunciationDrill, 2, true)]
    [InlineData(PremiumFeature.PronunciationDrill, 3, false)]
    public void Free_tier_allows_up_to_the_limit(PremiumFeature feature, int used, bool allowed)
    {
        var decision = EntitlementPolicy.Evaluate(feature, isPremium: false, usedInPeriod: used);

        decision.IsAllowed.Should().Be(allowed);
    }

    [Fact]
    public void Every_metered_feature_has_a_configured_free_limit()
    {
        // FreeLimitFor throws on an unmapped feature, so a member added without a limit would only
        // fail at runtime, on the learner's request.
        foreach (var feature in Enum.GetValues<PremiumFeature>())
        {
            var act = () => EntitlementPolicy.FreeLimitFor(feature);
            act.Should().NotThrow($"'{feature}' must declare a free-tier limit");
        }
    }

    [Theory]
    [InlineData(PremiumFeature.SpeakingSession, 0)]
    [InlineData(PremiumFeature.NewVocabularyWord, 1)]
    [InlineData(PremiumFeature.WritingAssessment, 2)]
    [InlineData(PremiumFeature.AssistantQuestion, 3)]
    [InlineData(PremiumFeature.PronunciationDrill, 4)]
    public void Feature_values_are_append_only(PremiumFeature feature, int expected)
    {
        // The numeric value is part of the persisted Redis usage key. Renumbering silently resets
        // every learner's allowance and makes one feature read another's counter.
        ((int)feature).Should().Be(expected);
    }

    [Fact]
    public void Premium_is_always_unlimited()
    {
        var decision = EntitlementPolicy.Evaluate(
            PremiumFeature.SpeakingSession, isPremium: true, usedInPeriod: 1000);

        decision.IsAllowed.Should().BeTrue();
        decision.Limit.Should().Be(GateDecision.Unlimited);
        decision.Remaining.Should().BeNull();
    }

    [Fact]
    public void Free_decision_reports_remaining_quota()
    {
        var decision = EntitlementPolicy.Evaluate(
            PremiumFeature.NewVocabularyWord, isPremium: false, usedInPeriod: 4);

        decision.Limit.Should().Be(10);
        decision.Remaining.Should().Be(6);
        decision.Period.Should().Be(UsagePeriod.Daily);
    }

    [Fact]
    public void Writing_assessment_resets_daily()
    {
        // Moved off a monthly window: three a month reads as "not included", while one a day is a
        // habit a free learner can build at a comparable cost.
        EntitlementPolicy.FreeLimitFor(PremiumFeature.WritingAssessment).Period
            .Should().Be(UsagePeriod.Daily);
    }
}
