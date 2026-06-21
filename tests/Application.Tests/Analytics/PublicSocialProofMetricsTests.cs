using Application.Analytics.GetPublicSocialProofMetrics;
using Application.Analytics.Ports;
using Application.Identity.Ports;
using Application.Subscription.Ports;
using Application.Tests.Learning;
using Domain.Analytics;
using Domain.Identity;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Analytics;

public class PublicSocialProofMetricsTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 28, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handler_returns_only_verified_aggregate_metrics()
    {
        var first = UserAccount.Register("sub-first", "first@example.com", "First", null, Now.AddDays(-50));
        var second = UserAccount.Register("sub-second", "second@example.com", "Second", null, Now.AddDays(-10));
        var accounts = Substitute.For<IUserAccountStore>();
        accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { first, second });

        var premium = Domain.Subscription.Subscription.CreateFree(first.Id, Now.AddDays(-30));
        premium.Activate(SubscriptionPlan.Monthly, Now.AddDays(-20));
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetPaidAsync(Arg.Any<CancellationToken>()).Returns(new[] { premium });

        var studyLogs = Substitute.For<IStudyLogStore>();
        studyLogs.GetActivityDaysAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(new[]
        {
            new StudyActivityDay(first.Id, DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-2)),
            new StudyActivityDay(first.Id, DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1)),
            new StudyActivityDay(second.Id, DateOnly.FromDateTime(Now.UtcDateTime)),
        });
        studyLogs.GetGlobalTotalsAsync(Arg.Any<CancellationToken>())
            .Returns(new GlobalStudyTotals(126_060, 18_000));

        var events = Substitute.For<IProductEventStore>();
        events.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            ProductEvent.Record(first.Id, ProductEventType.SpeakingSessionCompleted, Now.AddDays(-2), "session-1"),
            ProductEvent.Record(first.Id, ProductEventType.SpeakingSessionCompleted, Now.AddDays(-2), "session-1"),
            ProductEvent.Record(second.Id, ProductEventType.SpeakingSessionCompleted, Now.AddDays(-1), "session-2"),
            ProductEvent.Record(second.Id, ProductEventType.TopicOpened, Now, "topic-1"),
        });

        var handler = new GetPublicSocialProofMetricsQueryHandler(
            accounts, subscriptions, studyLogs, events, new FixedTimeProvider(Now));

        var result = await handler.Handle(new GetPublicSocialProofMetricsQuery(), CancellationToken.None);

        result.RegisteredUsers.Should().Be(2);
        result.ActivePremiumUsers.Should().Be(1);
        result.ActiveLearners30d.Should().Be(2);
        result.TotalStudyMinutes.Should().Be(2101);
        result.SpeakingMinutes.Should().Be(300);
        result.SpeakingSessions.Should().Be(2);
        await studyLogs.Received(1).GetActivityDaysAsync(
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-29), Arg.Any<CancellationToken>());
    }
}
