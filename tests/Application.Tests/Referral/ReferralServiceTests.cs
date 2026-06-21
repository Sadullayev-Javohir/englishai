using System.Collections.Concurrent;
using Application.Referral;
using Application.Referral.Ports;
using Application.Tests.Learning;
using Domain.Referral;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Referral;

/// <summary>
/// Tests the referral write-side rules: minting codes, capturing sign-ups, and paying the capped
/// reward to BOTH sides exactly once when a referred learner qualifies.
/// </summary>
public class ReferralServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeReferralStore _store = new();
    private readonly ReferralService _service;

    public ReferralServiceTests() => _service = new ReferralService(_store, new FixedTimeProvider(Now));

    [Fact]
    public async Task GetOrCreate_mints_a_valid_code_and_is_idempotent()
    {
        var learner = Guid.NewGuid();

        var first = await _service.GetOrCreateAccountAsync(learner, CancellationToken.None);
        var second = await _service.GetOrCreateAccountAsync(learner, CancellationToken.None);

        ReferralCode.IsValid(first.Code).Should().BeTrue();
        second.Id.Should().Be(first.Id);
        second.Code.Should().Be(first.Code);
    }

    [Fact]
    public async Task Capture_rewards_both_sides_immediately_on_signup()
    {
        var referrer = Guid.NewGuid();
        var referee = Guid.NewGuid();
        var referrerAccount = await _service.GetOrCreateAccountAsync(referrer, CancellationToken.None);

        // No qualification step - the reward must land the moment the friend signs up.
        await _service.CaptureAsync(referee, referrerAccount.Code, Now, CancellationToken.None);

        var referrerAfter = await _store.GetAccountByLearnerAsync(referrer, CancellationToken.None);
        var refereeAfter = await _store.GetAccountByLearnerAsync(referee, CancellationToken.None);

        referrerAfter!.BonusTopicAllowance.Should().Be(ReferralAccount.TopicsPerReferral);
        referrerAfter.RemainingSpeakingCredits.Should().Be(ReferralAccount.SpeakingCreditsPerReferral);
        referrerAfter.RemainingWritingCredits.Should().Be(ReferralAccount.WritingCreditsPerReferral);
        referrerAfter.RewardedCount.Should().Be(1);

        refereeAfter!.BonusTopicAllowance.Should().Be(ReferralAccount.TopicsPerReferral);
        refereeAfter.RemainingSpeakingCredits.Should().Be(ReferralAccount.SpeakingCreditsPerReferral);
        refereeAfter.RemainingWritingCredits.Should().Be(ReferralAccount.WritingCreditsPerReferral);
        refereeAfter.RewardedCount.Should().Be(1);

        // The referral is already qualified, and a later engagement signal must not pay again.
        var referral = await _store.GetReferralByRefereeAsync(referee, CancellationToken.None);
        referral!.Status.Should().Be(ReferralStatus.Qualified);

        await _service.TryQualifyAsync(referee, Now, CancellationToken.None);
        var referrerFinal = await _store.GetAccountByLearnerAsync(referrer, CancellationToken.None);
        referrerFinal!.RewardedCount.Should().Be(1);
    }

    [Fact]
    public async Task Capture_then_qualify_rewards_both_sides_once()
    {
        var referrer = Guid.NewGuid();
        var referee = Guid.NewGuid();
        var referrerAccount = await _service.GetOrCreateAccountAsync(referrer, CancellationToken.None);

        await _service.CaptureAsync(referee, referrerAccount.Code, Now, CancellationToken.None);
        await _service.TryQualifyAsync(referee, Now, CancellationToken.None);

        var referrerAfter = await _store.GetAccountByLearnerAsync(referrer, CancellationToken.None);
        var refereeAfter = await _store.GetAccountByLearnerAsync(referee, CancellationToken.None);

        referrerAfter!.BonusTopicAllowance.Should().Be(ReferralAccount.TopicsPerReferral);
        referrerAfter.RemainingSpeakingCredits.Should().Be(ReferralAccount.SpeakingCreditsPerReferral);
        referrerAfter.RemainingWritingCredits.Should().Be(ReferralAccount.WritingCreditsPerReferral);

        refereeAfter!.BonusTopicAllowance.Should().Be(ReferralAccount.TopicsPerReferral);

        // Qualifying again must not pay a second time.
        await _service.TryQualifyAsync(referee, Now, CancellationToken.None);
        var referrerFinal = await _store.GetAccountByLearnerAsync(referrer, CancellationToken.None);
        referrerFinal!.RewardedCount.Should().Be(1);
    }

    [Fact]
    public async Task Capture_ignores_an_unknown_code()
    {
        var referee = Guid.NewGuid();

        await _service.CaptureAsync(referee, "ZZZZZZ", Now, CancellationToken.None);

        (await _store.GetReferralByRefereeAsync(referee, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Capture_ignores_self_referral()
    {
        var learner = Guid.NewGuid();
        var account = await _service.GetOrCreateAccountAsync(learner, CancellationToken.None);

        await _service.CaptureAsync(learner, account.Code, Now, CancellationToken.None);

        (await _store.GetReferralByRefereeAsync(learner, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Capture_never_overwrites_an_existing_referral()
    {
        var referrer1 = await _service.GetOrCreateAccountAsync(Guid.NewGuid(), CancellationToken.None);
        var referrer2 = await _service.GetOrCreateAccountAsync(Guid.NewGuid(), CancellationToken.None);
        var referee = Guid.NewGuid();

        await _service.CaptureAsync(referee, referrer1.Code, Now, CancellationToken.None);
        await _service.CaptureAsync(referee, referrer2.Code, Now, CancellationToken.None);

        var referral = await _store.GetReferralByRefereeAsync(referee, CancellationToken.None);
        referral!.ReferrerId.Should().Be(referrer1.LearnerId);
    }

    [Fact]
    public async Task Qualify_is_a_noop_without_a_pending_referral()
    {
        var referee = Guid.NewGuid();

        await _service.TryQualifyAsync(referee, Now, CancellationToken.None);

        (await _store.GetAccountByLearnerAsync(referee, CancellationToken.None)).Should().BeNull();
    }

    /// <summary>Hand-written in-memory store (Application.Tests does not reference Infrastructure).</summary>
    private sealed class FakeReferralStore : IReferralStore
    {
        private readonly ConcurrentDictionary<Guid, ReferralAccount> _accounts = new();
        private readonly ConcurrentDictionary<Guid, Domain.Referral.Referral> _referrals = new();

        public Task<ReferralAccount?> GetAccountByLearnerAsync(Guid learnerId, CancellationToken ct)
        {
            _accounts.TryGetValue(learnerId, out var a);
            return Task.FromResult(a);
        }

        public Task<ReferralAccount?> GetAccountByCodeAsync(string normalizedCode, CancellationToken ct) =>
            Task.FromResult(_accounts.Values.FirstOrDefault(a => a.Code == normalizedCode));

        public Task SaveAccountAsync(ReferralAccount account, CancellationToken ct)
        {
            _accounts[account.LearnerId] = account;
            return Task.CompletedTask;
        }

        public Task<Domain.Referral.Referral?> GetReferralByRefereeAsync(Guid refereeId, CancellationToken ct)
        {
            _referrals.TryGetValue(refereeId, out var r);
            return Task.FromResult(r);
        }

        public Task<IReadOnlyList<Domain.Referral.Referral>> GetReferralsByReferrerAsync(
            Guid referrerId, CancellationToken ct)
        {
            IReadOnlyList<Domain.Referral.Referral> list =
                _referrals.Values.Where(r => r.ReferrerId == referrerId).ToList();
            return Task.FromResult(list);
        }

        public Task SaveReferralAsync(Domain.Referral.Referral referral, CancellationToken ct)
        {
            _referrals[referral.RefereeId] = referral;
            return Task.CompletedTask;
        }
    }
}
