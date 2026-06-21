using Application.Analytics.Ports;
using Application.Common;
using Application.Gamification.Ports;
using Application.Subscription.Access;
using Application.Subscription.CancelSubscription;
using Application.Subscription.ConfirmPayment;
using Application.Subscription.GetSubscription;
using Application.Subscription.Ports;
using Application.Subscription.StartSubscription;
using Application.Tests.Learning;
using Domain.Analytics;
using Domain.Common;
using Domain.Gamification;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Subscription;

public class SubscriptionHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IPaymentGatewayResolver _gateways = Substitute.For<IPaymentGatewayResolver>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IComplimentaryAccess _complimentary = Substitute.For<IComplimentaryAccess>();
    private readonly IProAccessService _proAccess = Substitute.For<IProAccessService>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();
    private readonly IDiscountRedemptionRepository _redemptions = Substitute.For<IDiscountRedemptionRepository>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static IPaymentAvailability Availability(bool enabled)
    {
        var availability = Substitute.For<IPaymentAvailability>();
        availability.PaymentsEnabled.Returns(enabled);
        return availability;
    }

    [Fact]
    public async Task Start_creates_a_pending_payment_and_returns_checkout()
    {
        _gateway.Provider.Returns(PaymentProvider.Click);
        _gateways.Resolve(PaymentProvider.Click).Returns(_gateway);
        _gateway.InitiatePaymentAsync(Arg.Any<PaymentInitiationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("tx-123", "/pay/tx-123"));

        Payment? saved = null;
        await _payments.SaveAsync(Arg.Do<Payment>(p => saved = p), Arg.Any<CancellationToken>());

        var handler = new StartSubscriptionCommandHandler(
            _gateways, _payments, Availability(enabled: true), _events, _redemptions, _clock);
        var result = await handler.Handle(
            new StartSubscriptionCommand(Learner, SubscriptionPlan.Monthly, PaymentProvider.Click, "http://app/profile", "http://api"), CancellationToken.None);

        result.TransactionId.Should().Be("tx-123");
        result.CheckoutUrl.Should().Be("/pay/tx-123");
        result.AmountUzs.Should().Be(SubscriptionPricing.MonthlyPriceUzs);
        result.Plan.Should().Be(SubscriptionPlan.Monthly);

        saved.Should().NotBeNull();
        saved!.Status.Should().Be(PaymentStatus.Pending);
        saved.TransactionId.Should().Be("tx-123");
        await _events.Received(1).AppendOnceAsync(
            Learner,
            ProductEventType.UpgradeClicked,
            Now,
            nameof(SubscriptionPlan.Monthly),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_is_refused_and_charges_nothing_when_payments_are_disabled()
    {
        _gateways.Resolve(PaymentProvider.Click).Returns(_gateway);
        var handler = new StartSubscriptionCommandHandler(
            _gateways, _payments, Availability(enabled: false), _events, _redemptions, _clock);

        var act = () => handler.Handle(
            new StartSubscriptionCommand(Learner, SubscriptionPlan.Monthly, PaymentProvider.Click, "http://app/profile", "http://api"), CancellationToken.None);

        await act.Should().ThrowAsync<PaymentsUnavailableException>();
        await _gateway.DidNotReceive().InitiatePaymentAsync(
            Arg.Any<PaymentInitiationRequest>(), Arg.Any<CancellationToken>());
        await _payments.DidNotReceive().SaveAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>());
        await _events.Received(1).AppendOnceAsync(
            Learner,
            ProductEventType.UpgradeClicked,
            Now,
            nameof(SubscriptionPlan.Monthly),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_with_a_valid_discount_code_reduces_the_charged_amount_and_consumes_it()
    {
        _gateway.Provider.Returns(PaymentProvider.Click);
        _gateways.Resolve(PaymentProvider.Click).Returns(_gateway);
        PaymentInitiationRequest? initiationRequest = null;
        _gateway.InitiatePaymentAsync(Arg.Do<PaymentInitiationRequest>(r => initiationRequest = r), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("tx-disc", "/pay/tx-disc"));

        var tier = DiscountCatalog.Tiers.First(t => t.DiscountPercent == 10);
        var redemption = DiscountRedemption.Create(Learner, tier, "SAVE1000", Now);
        _redemptions.GetByCodeAsync("SAVE1000", Arg.Any<CancellationToken>()).Returns(redemption);

        var handler = new StartSubscriptionCommandHandler(
            _gateways, _payments, Availability(enabled: true), _events, _redemptions, _clock);
        var result = await handler.Handle(
            new StartSubscriptionCommand(Learner, SubscriptionPlan.Monthly, PaymentProvider.Click, "http://app/profile", "http://api", "SAVE1000"), CancellationToken.None);

        var expectedAmount = DiscountCatalog.CalculateDiscountedAmount(
            SubscriptionPricing.MonthlyPriceUzs, 10);
        result.AmountUzs.Should().Be(expectedAmount);
        initiationRequest!.AmountUzs.Should().Be(expectedAmount);
        redemption.Status.Should().Be(DiscountRedemptionStatus.Used);
        await _redemptions.Received(1).SaveAsync(redemption, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_with_a_discount_code_owned_by_another_learner_throws_and_charges_nothing()
    {
        _gateways.Resolve(PaymentProvider.Click).Returns(_gateway);
        var tier = DiscountCatalog.Tiers.First(t => t.DiscountPercent == 10);
        var redemption = DiscountRedemption.Create(Guid.NewGuid(), tier, "NOTMINE", Now);
        _redemptions.GetByCodeAsync("NOTMINE", Arg.Any<CancellationToken>()).Returns(redemption);

        var handler = new StartSubscriptionCommandHandler(
            _gateways, _payments, Availability(enabled: true), _events, _redemptions, _clock);

        var act = () => handler.Handle(
            new StartSubscriptionCommand(Learner, SubscriptionPlan.Monthly, PaymentProvider.Click, "http://app/profile", "http://api", "NOTMINE"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _gateway.DidNotReceive().InitiatePaymentAsync(
            Arg.Any<PaymentInitiationRequest>(), Arg.Any<CancellationToken>());
        redemption.Status.Should().Be(DiscountRedemptionStatus.Active);
    }

    [Fact]
    public async Task Confirm_completes_payment_and_activates_premium()
    {
        var payment = Payment.Start(Learner, SubscriptionPlan.Monthly, PaymentProvider.Local, "tx-1", Now);
        _payments.GetByTransactionIdAsync("tx-1", Arg.Any<CancellationToken>()).Returns(payment);
        _subscriptions.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((Domain.Subscription.Subscription?)null);

        var handler = new ConfirmPaymentCommandHandler(_payments, _subscriptions, _events, _clock);
        var dto = await handler.Handle(new ConfirmPaymentCommand("tx-1"), CancellationToken.None);

        dto.Status.Should().Be(SubscriptionStatus.Premium);
        dto.IsPremiumActive.Should().BeTrue();
        dto.Plan.Should().Be(SubscriptionPlan.Monthly);
        payment.Status.Should().Be(PaymentStatus.Completed);
        await _subscriptions.Received(1).SaveAsync(Arg.Any<Domain.Subscription.Subscription>(), Arg.Any<CancellationToken>());
        await _events.Received(1).AppendOnceAsync(
            Learner,
            ProductEventType.PaymentCompleted,
            Now,
            nameof(SubscriptionPlan.Monthly),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirming_an_already_completed_payment_does_not_reactivate()
    {
        var payment = Payment.Start(Learner, SubscriptionPlan.Monthly, PaymentProvider.Local, "tx-2", Now);
        payment.Complete(Now);
        var subscription = Domain.Subscription.Subscription.CreateFree(Learner, Now);
        subscription.Activate(SubscriptionPlan.Monthly, Now);

        _payments.GetByTransactionIdAsync("tx-2", Arg.Any<CancellationToken>()).Returns(payment);
        _subscriptions.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(subscription);

        var handler = new ConfirmPaymentCommandHandler(_payments, _subscriptions, _events, _clock);
        await handler.Handle(new ConfirmPaymentCommand("tx-2"), CancellationToken.None);

        // No second activation: expiry stays one month out, not two.
        subscription.ExpiresAt.Should().Be(Now.AddDays(SubscriptionPricing.MonthlyDurationDays));
        await _subscriptions.DidNotReceive().SaveAsync(Arg.Any<Domain.Subscription.Subscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_marks_cancelled_but_keeps_access()
    {
        var subscription = Domain.Subscription.Subscription.CreateFree(Learner, Now);
        subscription.Activate(SubscriptionPlan.Monthly, Now);
        _subscriptions.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(subscription);

        var handler = new CancelSubscriptionCommandHandler(_subscriptions, _clock);
        var dto = await handler.Handle(new CancelSubscriptionCommand(Learner), CancellationToken.None);

        dto.Status.Should().Be(SubscriptionStatus.Cancelled);
        dto.IsPremiumActive.Should().BeTrue();
    }

    [Fact]
    public async Task Get_reports_free_for_an_unknown_learner_without_persisting()
    {
        _subscriptions.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((Domain.Subscription.Subscription?)null);

        _proAccess.EvaluateAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new ProAccessDecision(false, false, false, false, null));
        var handler = new GetSubscriptionQueryHandler(_subscriptions, _proAccess, _clock);
        var dto = await handler.Handle(new GetSubscriptionQuery(Learner), CancellationToken.None);

        dto.Status.Should().Be(SubscriptionStatus.Free);
        dto.IsPremiumActive.Should().BeFalse();
        await _subscriptions.DidNotReceive().SaveAsync(Arg.Any<Domain.Subscription.Subscription>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Get_reports_active_trial_without_marking_it_as_paid()
    {
        var expiry = Now.AddDays(30);
        _subscriptions.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((Domain.Subscription.Subscription?)null);
        _proAccess.EvaluateAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new ProAccessDecision(true, false, false, true, expiry));

        var handler = new GetSubscriptionQueryHandler(_subscriptions, _proAccess, _clock);
        var dto = await handler.Handle(new GetSubscriptionQuery(Learner), CancellationToken.None);

        dto.Status.Should().Be(SubscriptionStatus.Free);
        dto.IsPremiumActive.Should().BeFalse();
        dto.IsTrialActive.Should().BeTrue();
        dto.TrialExpiresAt.Should().Be(expiry);
        dto.TrialDaysUntilExpiry.Should().Be(30);
    }
}
