using Application.Common;
using Application.Identity.Ports;
using Application.Subscription.Access;
using Application.Subscription.Ports;
using Application.Tests.Learning;
using Domain.Identity;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Subscription;

public class ProAccessServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 23, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IComplimentaryAccess _complimentary = Substitute.For<IComplimentaryAccess>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();

    private ProAccessService CreateService(DateTimeOffset? now = null) =>
        new(_subscriptions, _complimentary, _accounts, new FixedTimeProvider(now ?? Now));

    [Fact]
    public async Task Newly_registered_account_has_full_access_during_trial()
    {
        var account = UserAccount.Register("sub", "user@example.com", "User", null, Now);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var decision = await CreateService().EvaluateAsync(account.Id, CancellationToken.None);

        decision.HasFullAccess.Should().BeTrue();
        decision.IsTrialActive.Should().BeTrue();
        decision.IsPaidPremium.Should().BeFalse();
        decision.TrialExpiresAt.Should().Be(Now.AddDays(UserAccount.ProTrialDurationDays));
    }

    [Fact]
    public async Task Trial_is_inactive_at_its_expiry_boundary()
    {
        var account = UserAccount.Register("sub", "user@example.com", "User", null, Now);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        var decision = await CreateService(account.ProTrialExpiresAt)
            .EvaluateAsync(account.Id, CancellationToken.None);

        decision.HasFullAccess.Should().BeFalse();
        decision.IsTrialActive.Should().BeFalse();
    }

    [Fact]
    public async Task Paid_subscription_has_full_access_after_trial()
    {
        var account = UserAccount.Register("sub", "user@example.com", "User", null, Now.AddDays(-40));
        var subscription = Domain.Subscription.Subscription.CreateFree(account.Id, Now);
        subscription.Activate(SubscriptionPlan.Monthly, Now);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        _subscriptions.GetByLearnerIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var decision = await CreateService().EvaluateAsync(account.Id, CancellationToken.None);

        decision.HasFullAccess.Should().BeTrue();
        decision.IsPaidPremium.Should().BeTrue();
        decision.IsTrialActive.Should().BeFalse();
    }

    [Fact]
    public async Task Complimentary_access_has_full_access_without_an_account()
    {
        _complimentary.HasFullAccessAsync(Learner, Arg.Any<CancellationToken>()).Returns(true);

        var decision = await CreateService().EvaluateAsync(Learner, CancellationToken.None);

        decision.HasFullAccess.Should().BeTrue();
        decision.IsComplimentary.Should().BeTrue();
    }
}
