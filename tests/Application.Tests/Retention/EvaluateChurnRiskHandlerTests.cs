using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Retention.EvaluateChurnRisk;
using Application.Subscription.Ports;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Retention;
using Domain.Subscription;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Retention;

/// <summary>
/// Unit tests for the per-learner churn-risk query (PROJECT-SPEC I.1): it assembles a
/// snapshot from the stores and delegates scoring to the domain evaluator.
/// </summary>
public class EvaluateChurnRiskHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IGamificationStore _gamification = Substitute.For<IGamificationStore>();
    private readonly IVocabularyRepository _vocabulary = Substitute.For<IVocabularyRepository>();
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    public EvaluateChurnRiskHandlerTests()
    {
        _gamification.GetCompletedDaysAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<DateOnly>());
        _vocabulary.GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<VocabularyItem>());
        _subscriptions.GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Subscription.Subscription?)null);
    }

    private EvaluateChurnRiskQueryHandler NewHandler() =>
        new(_profiles, _gamification, _vocabulary, _subscriptions, _clock);

    private static LearnerProfile ProfileInactiveFor(int days) =>
        LearnerProfile.CreateFromPlacement(
            Learner,
            new PlacementResult(CefrLevel.B1, CefrLevel.B1.ToScore(), new Dictionary<TestStage, StageResult>()),
            Now.AddDays(-days));

    [Fact]
    public async Task No_profile_means_onboarding_incomplete()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((LearnerProfile?)null);

        var result = await NewHandler().Handle(new EvaluateChurnRiskQuery(Learner), CancellationToken.None);

        result.OverallRisk.Should().Be(ChurnRiskLevel.VeryHigh);
        result.Signals.Should().ContainSingle().Which.Type.Should().Be(ChurnSignalType.OnboardingIncomplete);
    }

    [Fact]
    public async Task An_active_learner_has_no_risk()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(ProfileInactiveFor(0));

        var result = await NewHandler().Handle(new EvaluateChurnRiskQuery(Learner), CancellationToken.None);

        result.IsAtRisk.Should().BeFalse();
    }

    [Fact]
    public async Task Seven_days_inactive_yields_the_no_activity_signal()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(ProfileInactiveFor(7));

        var result = await NewHandler().Handle(new EvaluateChurnRiskQuery(Learner), CancellationToken.None);

        result.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.NoActivitySevenDays);
    }

    [Fact]
    public async Task A_two_day_streak_lapse_is_detected_from_completed_days()
    {
        var today = DateOnly.FromDateTime(Now.UtcDateTime);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(ProfileInactiveFor(2));
        // Met the goal up to two days ago, then missed yesterday and today.
        _gamification.GetCompletedDaysAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new List<DateOnly> { today.AddDays(-3), today.AddDays(-2) });

        var result = await NewHandler().Handle(new EvaluateChurnRiskQuery(Learner), CancellationToken.None);

        result.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.StreakBrokenTwoDays);
    }

    [Fact]
    public async Task Several_struggling_words_raise_the_srs_failure_signal()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(ProfileInactiveFor(1));
        _vocabulary.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(StrugglingWords(EvaluateChurnRiskQueryHandler.RisingSrsStrugglingWordCount));

        var result = await NewHandler().Handle(new EvaluateChurnRiskQuery(Learner), CancellationToken.None);

        result.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.RisingSrsFailures);
    }

    [Fact]
    public async Task Premium_expiring_soon_with_low_activity_is_flagged()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(ProfileInactiveFor(ChurnEvaluator.LowActivityDays));

        // Monthly plan activated 28 days ago → ~2 days left, within the window.
        var sub = Domain.Subscription.Subscription.CreateFree(Learner, Now.AddDays(-28));
        sub.Activate(SubscriptionPlan.Monthly, Now.AddDays(-28));
        _subscriptions.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(sub);

        var result = await NewHandler().Handle(new EvaluateChurnRiskQuery(Learner), CancellationToken.None);

        result.Signals.Select(s => s.Type).Should().Contain(ChurnSignalType.PremiumLapsingWithLowActivity);
    }

    private static List<VocabularyItem> StrugglingWords(int count)
    {
        var words = new List<VocabularyItem>();
        for (var i = 0; i < count; i++)
        {
            var item = VocabularyItem.Learn(Learner, $"word{i}", $"soz{i}", Now.AddDays(-30));
            // Two failed reviews push FailCount to the struggling threshold.
            item.RecordReview(false, Now.AddDays(-3));
            item.RecordReview(false, Now.AddDays(-2));
            words.Add(item);
        }
        return words;
    }
}
