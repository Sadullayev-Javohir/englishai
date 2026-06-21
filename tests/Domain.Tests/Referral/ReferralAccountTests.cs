using Domain.Referral;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Referral;

/// <summary>
/// Unit tests for <see cref="ReferralAccount"/>: the reward bundle, the lifetime cap that keeps
/// the programme cheap, and consuming the bonus Speaking/Writing credits.
/// </summary>
public class ReferralAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    private static ReferralAccount NewAccount() =>
        ReferralAccount.Create(Guid.NewGuid(), ReferralCode.Generate(), Now);

    [Fact]
    public void Granting_a_reward_adds_the_full_bundle()
    {
        var account = NewAccount();

        var granted = account.GrantReferralReward(Now);

        granted.Should().BeTrue();
        account.RewardedCount.Should().Be(1);
        account.BonusTopicAllowance.Should().Be(ReferralAccount.TopicsPerReferral);
        account.RemainingSpeakingCredits.Should().Be(ReferralAccount.SpeakingCreditsPerReferral);
        account.RemainingWritingCredits.Should().Be(ReferralAccount.WritingCreditsPerReferral);
    }

    [Fact]
    public void Rewards_accumulate_up_to_the_cap_then_stop()
    {
        var account = NewAccount();

        for (var i = 0; i < ReferralAccount.MaxRewardedReferrals; i++)
            account.GrantReferralReward(Now).Should().BeTrue();

        // The cap is reached - a further grant is refused and balances stay put.
        account.HasReachedRewardCap.Should().BeTrue();
        account.GrantReferralReward(Now).Should().BeFalse();

        account.RewardedCount.Should().Be(ReferralAccount.MaxRewardedReferrals);
        account.BonusTopicAllowance.Should()
            .Be(ReferralAccount.MaxRewardedReferrals * ReferralAccount.TopicsPerReferral);
    }

    [Fact]
    public void Consuming_speaking_credits_decrements_until_empty()
    {
        var account = NewAccount();
        account.GrantReferralReward(Now); // +3 speaking

        for (var i = 0; i < ReferralAccount.SpeakingCreditsPerReferral; i++)
            account.ConsumeSpeakingCredit(Now).Should().BeTrue();

        account.RemainingSpeakingCredits.Should().Be(0);
        account.ConsumeSpeakingCredit(Now).Should().BeFalse();
    }

    [Fact]
    public void Consuming_writing_credits_decrements_until_empty()
    {
        var account = NewAccount();
        account.GrantReferralReward(Now); // +2 writing

        for (var i = 0; i < ReferralAccount.WritingCreditsPerReferral; i++)
            account.ConsumeWritingCredit(Now).Should().BeTrue();

        account.RemainingWritingCredits.Should().Be(0);
        account.ConsumeWritingCredit(Now).Should().BeFalse();
    }

    [Fact]
    public void Create_rejects_an_empty_learner()
    {
        var act = () => ReferralAccount.Create(Guid.Empty, ReferralCode.Generate(), Now);

        act.Should().Throw<Domain.Common.DomainException>();
    }
}
