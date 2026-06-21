using Application.Notifications;
using Application.Notifications.Ports;
using Application.Subscription.Ports;
using Application.Subscription.ProcessExpiry;
using Application.Tests.Learning;
using Domain.Notifications;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Subscription;

public class ProcessSubscriptionExpiryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly INotificationDispatcher _dispatcher = Substitute.For<INotificationDispatcher>();
    private readonly INotificationTemplateProvider _templates = Substitute.For<INotificationTemplateProvider>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    public ProcessSubscriptionExpiryHandlerTests()
    {
        _templates.Get(Arg.Any<string>(), Arg.Any<IReadOnlyDictionary<string, string>?>())
            .Returns(ci => ci.ArgAt<string>(0)); // echo the code as message
    }

    private static Domain.Subscription.Subscription Premium(DateTimeOffset activatedAt, SubscriptionPlan plan)
    {
        var sub = Domain.Subscription.Subscription.CreateFree(Guid.NewGuid(), activatedAt);
        sub.Activate(plan, activatedAt);
        return sub;
    }

    [Fact]
    public async Task Sends_a_reminder_for_a_subscription_expiring_within_the_window()
    {
        // Activated 28 days ago on a monthly plan → ~2 days left, within the 3-day window.
        var sub = Premium(Now.AddDays(-28), SubscriptionPlan.Monthly);
        _subscriptions.GetPaidAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Subscription.Subscription> { sub });

        var handler = NewHandler();
        var result = await handler.Handle(new ProcessSubscriptionExpiryCommand(), CancellationToken.None);

        result.RemindedLearners.Should().Be(1);
        result.ExpiredSubscriptions.Should().Be(0);
        await _dispatcher.Received(1).SendAsync(
            Arg.Is<Notification>(n => n.Code == NotificationCodes.SubscriptionExpiringSoon),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Expires_a_lapsed_subscription_and_notifies()
    {
        var sub = Premium(Now.AddDays(-40), SubscriptionPlan.Monthly); // lapsed 10 days ago
        _subscriptions.GetPaidAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Subscription.Subscription> { sub });

        var handler = NewHandler();
        var result = await handler.Handle(new ProcessSubscriptionExpiryCommand(), CancellationToken.None);

        result.ExpiredSubscriptions.Should().Be(1);
        sub.Status.Should().Be(SubscriptionStatus.Expired);
        await _subscriptions.Received(1).SaveAsync(sub, Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).SendAsync(
            Arg.Is<Notification>(n => n.Code == NotificationCodes.SubscriptionExpired),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaves_a_healthy_subscription_untouched()
    {
        var sub = Premium(Now, SubscriptionPlan.Yearly); // ~365 days left
        _subscriptions.GetPaidAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Domain.Subscription.Subscription> { sub });

        var handler = NewHandler();
        var result = await handler.Handle(new ProcessSubscriptionExpiryCommand(), CancellationToken.None);

        result.Should().Be(new ProcessExpiryResultDto(0, 0));
        await _dispatcher.DidNotReceive().SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    private ProcessSubscriptionExpiryCommandHandler NewHandler() =>
        new(_subscriptions, _dispatcher, _templates, _clock);
}
