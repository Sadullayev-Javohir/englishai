using Application.Common;
using Application.Referral.Ports;
using Application.Subscription.Access;
using Application.Subscription.Entitlements;
using Application.Subscription.Ports;
using Application.Tests.Learning;
using Domain.Referral;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Subscription;

public class EntitlementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IProAccessService _proAccess = Substitute.For<IProAccessService>();
    private readonly FakeUsageCounter _usage = new();
    private readonly IReferralStore _referrals = Substitute.For<IReferralStore>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private EntitlementService NewService() => new(_proAccess, _usage, _referrals, _clock);

    private void GivenComplimentary() => GivenFullAccess(isComplimentary: true);

    private void GivenFree() =>
        _proAccess.EvaluateAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new ProAccessDecision(false, false, false, false, null));

    private void GivenPremium() => GivenFullAccess();

    private void GivenFullAccess(bool isComplimentary = false, bool isTrial = false) =>
        _proAccess.EvaluateAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new ProAccessDecision(true, !isComplimentary && !isTrial, isComplimentary, isTrial,
                isTrial ? Now.AddDays(30) : null));

    [Fact]
    public async Task Free_user_under_the_limit_is_allowed_and_usage_is_recorded()
    {
        GivenFree();
        var service = NewService();

        var decision = await service.EvaluateAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);
        decision.IsAllowed.Should().BeTrue();

        var freeLimit = EntitlementPolicy.FreeLimitFor(PremiumFeature.SpeakingSession).MaxUses;
        for (var used = 0; used < freeLimit; used++)
            await service.RecordUsageAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        // Once the day's sessions are spent, the next is denied.
        var act = async () =>
            await service.EnsureAllowedAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);
        await act.Should().ThrowAsync<FeatureLimitExceededException>();
    }

    [Fact]
    public async Task Premium_user_is_never_limited()
    {
        GivenPremium();
        var service = NewService();

        // Simulate heavy usage.
        for (var i = 0; i < 50; i++)
            await service.RecordUsageAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        var decision = await service.EnsureAllowedAsync(
            Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
        decision.Limit.Should().Be(GateDecision.Unlimited);
    }

    [Fact]
    public async Task Active_Pro_trial_is_never_limited()
    {
        GivenFullAccess(isTrial: true);
        var service = NewService();

        for (var i = 0; i < 50; i++)
            await service.RecordUsageAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        var decision = await service.EnsureAllowedAsync(
            Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
        decision.Limit.Should().Be(GateDecision.Unlimited);
    }

    [Fact]
    public async Task Complimentary_account_is_never_limited_even_on_the_free_tier()
    {
        GivenFree();
        GivenComplimentary();
        var service = NewService();

        // Burn well past every free limit; the comped account stays unlimited.
        for (var i = 0; i < 50; i++)
            await service.RecordUsageAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        var decision = await service.EnsureAllowedAsync(
            Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
        decision.Limit.Should().Be(GateDecision.Unlimited);
    }

    [Fact]
    public async Task Usage_is_isolated_per_feature()
    {
        GivenFree();
        var service = NewService();

        // Spend the speaking quota; vocabulary quota is untouched.
        await service.RecordUsageAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        var vocab = await service.EvaluateAsync(Learner, PremiumFeature.NewVocabularyWord, CancellationToken.None);
        vocab.IsAllowed.Should().BeTrue();
        vocab.Used.Should().Be(0);
    }

    [Fact]
    public async Task Referral_bonus_lets_a_free_user_exceed_the_speaking_limit_and_draws_down_a_credit()
    {
        GivenFree();
        var account = ReferralAccount.Create(Learner, ReferralCode.Generate(), Now);
        account.GrantReferralReward(Now); // +3 bonus Speaking credits
        _referrals.GetAccountByLearnerAsync(Learner, Arg.Any<CancellationToken>()).Returns(account);
        var service = NewService();

        // Spend the day's free sessions.
        var freeLimit = EntitlementPolicy.FreeLimitFor(PremiumFeature.SpeakingSession).MaxUses;
        for (var used = 0; used < freeLimit; used++)
            await service.RecordUsageAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);

        // Free allowance is gone, but a referral bonus credit still permits one more.
        var decision = await service.EnsureAllowedAsync(
            Learner, PremiumFeature.SpeakingSession, CancellationToken.None);
        decision.IsAllowed.Should().BeTrue();

        // Recording now consumes a bonus credit rather than the (already-full) period counter.
        await service.RecordUsageAsync(Learner, PremiumFeature.SpeakingSession, CancellationToken.None);
        account.RemainingSpeakingCredits.Should().Be(ReferralAccount.SpeakingCreditsPerReferral - 1);
    }

    [Fact]
    public async Task Referral_bonus_does_not_apply_to_vocabulary()
    {
        GivenFree();
        var account = ReferralAccount.Create(Learner, ReferralCode.Generate(), Now);
        account.GrantReferralReward(Now);
        _referrals.GetAccountByLearnerAsync(Learner, Arg.Any<CancellationToken>()).Returns(account);
        var service = NewService();

        // Exhaust the 10/day vocabulary free limit.
        for (var i = 0; i < 10; i++)
            await service.RecordUsageAsync(Learner, PremiumFeature.NewVocabularyWord, CancellationToken.None);

        // No bonus pool exists for vocabulary, so the 11th is denied despite bonus credits elsewhere.
        var act = async () =>
            await service.EnsureAllowedAsync(Learner, PremiumFeature.NewVocabularyWord, CancellationToken.None);
        await act.Should().ThrowAsync<FeatureLimitExceededException>();
    }

    private sealed class FakeUsageCounter : IUsageCounter
    {
        private readonly Dictionary<string, int> _counts = new();

        private static string Key(Guid learnerId, PremiumFeature feature, string periodKey) =>
            $"{learnerId:N}:{(int)feature}:{periodKey}";

        public Task<int> GetCountAsync(
            Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken)
        {
            _counts.TryGetValue(Key(learnerId, feature, periodKey), out var count);
            return Task.FromResult(count);
        }

        public Task IncrementAsync(
            Guid learnerId, PremiumFeature feature, string periodKey, CancellationToken cancellationToken)
        {
            var key = Key(learnerId, feature, periodKey);
            _counts.TryGetValue(key, out var current);
            _counts[key] = current + 1;
            return Task.CompletedTask;
        }
    }
}
