using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Subscription;

/// <summary>
/// Unit tests for the trial paywall policy (PROJECT-SPEC H.1): the first
/// <see cref="FreeTopicAccessPolicy.FreeTopicAllowance"/> topics are free; later topics require
/// Premium, while privileged learners (Premium/comped) have unlimited access.
/// </summary>
public class FreeTopicAccessPolicyTests
{
    private static readonly Guid TopicA = Guid.NewGuid();
    private static readonly Guid TopicB = Guid.NewGuid();
    private static readonly Guid TopicC = Guid.NewGuid();
    private static readonly Guid TopicD = Guid.NewGuid();

    [Fact]
    public void Free_learner_can_start_the_first_topic()
    {
        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: false, startedTopicIdsEarliestFirst: Array.Empty<Guid>(), requestedTopicId: TopicA);

        decision.IsAllowed.Should().BeTrue();
        decision.RequiresPro.Should().BeFalse();
    }

    [Fact]
    public void Free_learner_can_start_the_second_topic()
    {
        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: false, startedTopicIdsEarliestFirst: new[] { TopicA }, requestedTopicId: TopicB);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Free_learner_is_blocked_on_the_fourth_topic()
    {
        // Three topics already started - the trial is used up, so a new fourth topic needs Premium.
        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: false,
            startedTopicIdsEarliestFirst: new[] { TopicA, TopicB, TopicC },
            requestedTopicId: TopicD);

        decision.IsAllowed.Should().BeFalse();
        decision.RequiresPro.Should().BeTrue();
        decision.FreeAllowance.Should().Be(FreeTopicAccessPolicy.FreeTopicAllowance);
    }

    [Fact]
    public void Free_learner_keeps_access_to_an_already_started_free_topic()
    {
        // Re-opening one of the first three topics stays free even though the trial count is at the cap.
        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: false,
            startedTopicIdsEarliestFirst: new[] { TopicA, TopicB, TopicC },
            requestedTopicId: TopicA);

        decision.IsAllowed.Should().BeTrue();
        decision.RequiresPro.Should().BeFalse();
    }

    [Fact]
    public void A_started_topic_outside_the_earliest_three_is_locked_again()
    {
        // Possible after a lapsed Premium period: only the three oldest started topics remain free.
        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: false,
            startedTopicIdsEarliestFirst: new[] { TopicA, TopicB, TopicC, TopicD },
            requestedTopicId: TopicD);

        decision.IsAllowed.Should().BeFalse();
        decision.RequiresPro.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    public void Privileged_learner_has_unlimited_access(int startedCount)
    {
        var started = Enumerable.Range(0, startedCount).Select(_ => Guid.NewGuid()).ToList();

        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: true, startedTopicIdsEarliestFirst: started, requestedTopicId: TopicC);

        decision.IsAllowed.Should().BeTrue();
        decision.RequiresPro.Should().BeFalse();
    }

    [Fact]
    public void Referral_bonus_widens_the_free_allowance()
    {
        // Base allowance is spent, but a +2 referral bonus unlocks a fourth topic.
        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: false,
            startedTopicIdsEarliestFirst: new[] { TopicA, TopicB, TopicC },
            requestedTopicId: TopicD,
            bonusTopicAllowance: 2);

        decision.IsAllowed.Should().BeTrue();
        decision.RequiresPro.Should().BeFalse();
        decision.FreeAllowance.Should().Be(FreeTopicAccessPolicy.FreeTopicAllowance + 2);
    }

    [Fact]
    public void Free_learner_is_blocked_once_even_the_bonus_allowance_is_spent()
    {
        // Base 3 + bonus 1 = 4 free topics; a fifth still needs Premium.
        var started = new[] { TopicA, TopicB, TopicC, TopicD };

        var decision = FreeTopicAccessPolicy.Evaluate(
            hasFullAccess: false,
            startedTopicIdsEarliestFirst: started,
            requestedTopicId: Guid.NewGuid(),
            bonusTopicAllowance: 1);

        decision.IsAllowed.Should().BeFalse();
        decision.RequiresPro.Should().BeTrue();
    }
}
