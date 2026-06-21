using Application.Analytics.GetFounderMetrics;
using Application.Ai;
using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Subscription.Ports;
using Application.Tests.Learning;
using Domain.Analytics;
using Domain.Identity;
using Domain.Learning;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Analytics;

/// <summary>
/// Covers the founder growth dashboard: the pure calculator's DAU/retention/conversion math and the
/// handler's admin gate. The calculator is exercised directly (no mocks) against a fixed as-of day so
/// the windows are deterministic; the handler tests only the 403 gate + wiring (substituted stores).
/// </summary>
public class FounderMetricsTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 27, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private static UserAccount AccountRegisteredOn(DateOnly day, string email)
    {
        var at = new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return UserAccount.Register($"sub-{email}", email, "User", null, at);
    }

    [Fact]
    public void Calculator_computes_active_users_signups_conversion_and_mrr()
    {
        var u1 = AccountRegisteredOn(Today.AddDays(-1), "u1@example.com");
        var u2 = AccountRegisteredOn(Today.AddDays(-1), "u2@example.com");
        var u3 = AccountRegisteredOn(Today, "u3@example.com");
        var accounts = new[] { u1, u2, u3 };

        // u3 is a paying learner (activates Premium now).
        var premium = Domain.Subscription.Subscription.CreateFree(u3.Id, Now);
        premium.Activate(SubscriptionPlan.Monthly, Now);

        var activity = new[]
        {
            new StudyActivityDay(u1.Id, Today.AddDays(-1)),
            new StudyActivityDay(u1.Id, Today),
            new StudyActivityDay(u3.Id, Today),
        };

        var metrics = FounderMetricsCalculator.Build(
            Today, Now, accounts, Array.Empty<LearnerProfile>(), new[] { premium }, activity,
            Array.Empty<ProductEvent>());

        metrics.AsOf.Should().Be(Today);
        metrics.Overview.TotalUsers.Should().Be(3);
        metrics.Overview.Dau.Should().Be(2);           // u1 + u3 active today
        metrics.Overview.Wau.Should().Be(2);
        metrics.Overview.Mau.Should().Be(2);
        metrics.Overview.NewUsersToday.Should().Be(1); // u3
        metrics.Overview.NewUsers7d.Should().Be(3);

        metrics.Overview.PremiumUsers.Should().Be(1);
        metrics.Overview.FreeUsers.Should().Be(2);
        metrics.Overview.ConversionRatePct.Should().BeApproximately(33.3, 0.1);
        metrics.Overview.MrrUzs.Should().Be(SubscriptionPricing.MonthlyPriceUzs);
        metrics.Overview.ArppuUzs.Should().Be(SubscriptionPricing.MonthlyPriceUzs);

        // Trends span the fixed 30-day window and end on the as-of day.
        metrics.ActiveUsersTrend.Should().HaveCount(FounderMetricsCalculator.TrendWindowDays);
        metrics.ActiveUsersTrend[^1].Should().Be(new Application.Analytics.Dtos.DailyCountDto(Today, 2));
        metrics.SignupsTrend[^1].Count.Should().Be(1); // one signup today

        metrics.Funnel.Registered.Should().Be(3);
        metrics.Funnel.Active7d.Should().Be(2);
        metrics.Funnel.Premium.Should().Be(1);

        metrics.PlanBreakdown.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                Plan = nameof(SubscriptionPlan.Monthly),
                Count = 1,
                MrrUzs = (long)SubscriptionPricing.MonthlyPriceUzs,
            });
        metrics.Demographics.Completed.Should().Be(0);
        metrics.Demographics.Missing.Should().Be(3);
    }

    [Fact]
    public void Calculator_measures_day_one_retention_for_eligible_cohorts()
    {
        // Both registered yesterday, so day 1 has elapsed and they are in the D1 cohort.
        var retained = AccountRegisteredOn(Today.AddDays(-1), "retained@example.com");
        var churned = AccountRegisteredOn(Today.AddDays(-1), "churned@example.com");
        // Registered today: day 1 has not elapsed yet, so excluded from the D1 cohort.
        var brandNew = AccountRegisteredOn(Today, "new@example.com");

        var activity = new[]
        {
            // retained came back on registration day + 1 (= today).
            new StudyActivityDay(retained.Id, Today),
            // brandNew was active today but is not measurable for D1 yet.
            new StudyActivityDay(brandNew.Id, Today),
        };

        var metrics = FounderMetricsCalculator.Build(
            Today, Now, new[] { retained, churned, brandNew },
            Array.Empty<LearnerProfile>(), Array.Empty<Domain.Subscription.Subscription>(), activity,
            Array.Empty<ProductEvent>());

        metrics.Retention.D1.CohortSize.Should().Be(2); // retained + churned, not brandNew
        metrics.Retention.D1.Retained.Should().Be(1);
        metrics.Retention.D1.RatePct.Should().BeApproximately(50.0, 0.1);

        // No cohort is old enough for D7/D30 in this fixture.
        metrics.Retention.D7.CohortSize.Should().Be(0);
        metrics.Retention.D30.CohortSize.Should().Be(0);
    }

    [Fact]
    public void Calculator_builds_sequential_activation_funnel_from_product_events()
    {
        var learner = AccountRegisteredOn(Today.AddDays(-8), "activated@example.com");
        learner.SetUsername("activated_user");
        var dropped = AccountRegisteredOn(Today.AddDays(-8), "dropped@example.com");
        dropped.SetUsername("dropped_user");

        var events = new[]
        {
            ProductEvent.Record(learner.Id, ProductEventType.PlacementStarted, Now.AddDays(-8), "placement-1"),
            ProductEvent.Record(learner.Id, ProductEventType.PlacementCompleted, Now.AddDays(-8), "placement-1"),
            ProductEvent.Record(learner.Id, ProductEventType.TopicOpened, Now.AddDays(-7), "topic-1"),
            ProductEvent.Record(learner.Id, ProductEventType.SpeakingSessionCompleted, Now.AddDays(-7), "session-1"),
            ProductEvent.Record(learner.Id, ProductEventType.PronunciationFeedbackViewed, Now.AddDays(-7), "session-1"),
            ProductEvent.Record(learner.Id, ProductEventType.SignedIn, Now.AddDays(-7), "google"),
            ProductEvent.Record(learner.Id, ProductEventType.SpeakingSessionCompleted, Now.AddDays(-6), "session-2"),
            ProductEvent.Record(learner.Id, ProductEventType.SpeakingSessionCompleted, Now.AddDays(-5), "session-3"),
            ProductEvent.Record(learner.Id, ProductEventType.PaywallHit, Now.AddDays(-4), "topic-3"),

            // This user set a username but never started placement; later steps must stay sequential.
            ProductEvent.Record(dropped.Id, ProductEventType.UsernameSetupCompleted, Now.AddDays(-8), "profile"),
        };

        var metrics = FounderMetricsCalculator.Build(
            Today,
            Now,
            new[] { learner, dropped },
            Array.Empty<LearnerProfile>(),
            Array.Empty<Domain.Subscription.Subscription>(),
            Array.Empty<StudyActivityDay>(),
            events);

        metrics.ActivationFunnel.Select(s => new { s.Code, s.Count }).Should().Equal(
            new { Code = "registered", Count = 2 },
            new { Code = "usernameSetupCompleted", Count = 2 },
            new { Code = "placementStarted", Count = 1 },
            new { Code = "placementCompleted", Count = 1 },
            new { Code = "firstTopicOpened", Count = 1 },
            new { Code = "firstSpeakingSessionCompleted", Count = 1 },
            new { Code = "firstPronunciationFeedbackViewed", Count = 1 },
            new { Code = "day1Returned", Count = 1 },
            new { Code = "day7Active", Count = 1 },
            new { Code = "monetizationIntent", Count = 1 });

        metrics.ActivationFunnel[2].RateFromPreviousPct.Should().Be(50);
        metrics.ActivationFunnel[^1].RateFromRegisteredPct.Should().Be(50);
    }

    [Fact]
    public async Task Handler_forbids_a_non_admin()
    {
        var admin = Substitute.For<IAdminAuthorization>();
        admin.GetRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(AdminRole.None);

        var handler = new GetFounderMetricsQueryHandler(
            admin,
            Substitute.For<IUserAccountStore>(),
            Substitute.For<ILearnerProfileRepository>(),
            Substitute.For<ISubscriptionRepository>(),
            Substitute.For<IStudyLogStore>(),
            Substitute.For<IProductEventStore>(),
            new FixedTimeProvider(Now));

        var act = () => handler.Handle(new GetFounderMetricsQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handler_returns_metrics_for_an_admin()
    {
        var admin = Substitute.For<IAdminAuthorization>();
        admin.GetRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(AdminRole.Admin);

        var accounts = Substitute.For<IUserAccountStore>();
        accounts.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { AccountRegisteredOn(Today, "a@example.com") });

        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<LearnerProfile>());

        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetPaidAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Domain.Subscription.Subscription>());

        var studyLog = Substitute.For<IStudyLogStore>();
        studyLog.GetActivityDaysAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StudyActivityDay>());

        var productEvents = Substitute.For<IProductEventStore>();
        productEvents.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<ProductEvent>());

        var costs = Substitute.For<IVariableCostMeter>();
        var costSnapshot = new VariableCostSnapshot(
            Today,
            3.5,
            25,
            false,
            false,
            true,
            Array.Empty<VariableCostCategorySnapshot>(),
            Array.Empty<VariableCostAttributionSnapshot>(),
            Array.Empty<VariableCostAttributionSnapshot>());
        costs.Snapshot().Returns(costSnapshot);
        var handler = new GetFounderMetricsQueryHandler(
            admin, accounts, profiles, subscriptions, studyLog, productEvents, new FixedTimeProvider(Now), costs);

        var result = await handler.Handle(new GetFounderMetricsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Overview.TotalUsers.Should().Be(1);
        result.Overview.NewUsersToday.Should().Be(1);
        result.VariableCosts.Should().BeSameAs(costSnapshot);
        result.CanManageVariableCostBudget.Should().BeFalse();

        // Activity is read from the bounded look-back window, not all history.
        await studyLog.Received(1).GetActivityDaysAsync(
            Today.AddDays(-FounderMetricsCalculator.RetentionLookbackDays), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handler_uses_requested_trend_range_and_reads_enough_activity_history()
    {
        var admin = Substitute.For<IAdminAuthorization>();
        admin.GetRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(AdminRole.Admin);
        var accounts = Substitute.For<IUserAccountStore>();
        accounts.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { AccountRegisteredOn(Today.AddDays(-120), "old@example.com") });
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<LearnerProfile>());
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetPaidAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Domain.Subscription.Subscription>());
        var studyLog = Substitute.For<IStudyLogStore>();
        studyLog.GetActivityDaysAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudyActivityDay>());
        var events = Substitute.For<IProductEventStore>();
        events.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<ProductEvent>());
        var handler = new GetFounderMetricsQueryHandler(admin, accounts, profiles, subscriptions, studyLog, events, new FixedTimeProvider(Now));
        var from = Today.AddDays(-90);

        var result = await handler.Handle(new GetFounderMetricsQuery(Guid.NewGuid(), from, Today), CancellationToken.None);

        result.ActiveUsersTrend.Should().HaveCount(91);
        result.ActiveUsersTrend[0].Day.Should().Be(from);
        await studyLog.Received(1).GetActivityDaysAsync(from, Arg.Any<CancellationToken>());
    }
}
