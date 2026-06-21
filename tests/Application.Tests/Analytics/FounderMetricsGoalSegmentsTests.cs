using Application.Analytics.GetFounderMetrics;
using Domain.Analytics;
using Domain.Assessment;
using Domain.Common;
using Domain.Identity;
using Domain.Learning;
using Domain.Subscription;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Analytics;

/// <summary>
/// Covers the per-goal segment breakdown (goal-based onboarding) added to the founder dashboard:
/// each learning goal reports its user count, paying users and conversion, so the operator can see
/// which segment monetizes best. The goal now lives on the learner profile, so segments are resolved
/// by joining accounts to their profile's goal (accounts without a profile fall into Unspecified).
/// </summary>
public class FounderMetricsGoalSegmentsTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 27, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private static UserAccount Account(string email)
    {
        return UserAccount.Register($"sub-{email}", email, "User", null, Now.AddDays(-1));
    }

    private static LearnerProfile ProfileWithGoal(Guid learnerId, LearningGoal goal)
    {
        var profile = LearnerProfile.CreateAtLevel(learnerId, CefrLevel.A2, Now.AddDays(-1));
        profile.SetLearningGoal(goal);
        return profile;
    }

    [Fact]
    public void Build_reports_users_and_conversion_per_goal_segment()
    {
        var ielts1 = Account("i1@example.com");
        var ielts2 = Account("i2@example.com");
        var work1 = Account("w1@example.com");
        var accounts = new[] { ielts1, ielts2, work1 };

        var profiles = new[]
        {
            ProfileWithGoal(ielts1.Id, LearningGoal.IeltsCefr),
            ProfileWithGoal(ielts2.Id, LearningGoal.IeltsCefr),
            ProfileWithGoal(work1.Id, LearningGoal.Work),
        };

        // One IELTS learner pays; nobody in the Work segment does.
        var premium = Domain.Subscription.Subscription.CreateFree(ielts1.Id, Now);
        premium.Activate(SubscriptionPlan.Monthly, Now);

        var metrics = FounderMetricsCalculator.Build(
            Today, Now, accounts, profiles, new[] { premium },
            Array.Empty<StudyActivityDay>(), Array.Empty<ProductEvent>());

        var ielts = metrics.GoalSegments.Single(s => s.Goal == nameof(LearningGoal.IeltsCefr));
        ielts.Users.Should().Be(2);
        ielts.PremiumUsers.Should().Be(1);
        ielts.ConversionRatePct.Should().BeApproximately(50.0, 0.1);
        ielts.MrrUzs.Should().Be(SubscriptionPricing.MonthlyPriceUzs);

        var work = metrics.GoalSegments.Single(s => s.Goal == nameof(LearningGoal.Work));
        work.Users.Should().Be(1);
        work.PremiumUsers.Should().Be(0);
        work.ConversionRatePct.Should().Be(0);
        work.MrrUzs.Should().Be(0);
    }

    [Fact]
    public void Build_includes_the_unspecified_segment_for_accounts_without_a_goal()
    {
        // An account with no learner profile yet (not onboarded) has no goal → Unspecified segment.
        var legacy = Account("legacy@example.com");

        var metrics = FounderMetricsCalculator.Build(
            Today, Now, new[] { legacy }, Array.Empty<LearnerProfile>(),
            Array.Empty<Domain.Subscription.Subscription>(),
            Array.Empty<StudyActivityDay>(), Array.Empty<ProductEvent>());

        metrics.GoalSegments.Should().ContainSingle()
            .Which.Goal.Should().Be(nameof(LearningGoal.Unspecified));
    }
}
