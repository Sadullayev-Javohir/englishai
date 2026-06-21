using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Retention.Dtos;
using Application.Subscription.Ports;
using Application.Vocabulary.Ports;
using Domain.Gamification;
using Domain.Retention;
using MediatR;

namespace Application.Retention.EvaluateChurnRisk;

/// <summary>
/// Builds a <see cref="ChurnInputs"/> snapshot for a learner from the various stores and
/// runs the pure <see cref="ChurnEvaluator"/> (PROJECT-SPEC I.1). The gathering lives here;
/// the scoring stays in the domain so it is independently unit-tested.
/// </summary>
public sealed class EvaluateChurnRiskQueryHandler
    : IRequestHandler<EvaluateChurnRiskQuery, ChurnAssessmentDto>
{
    /// <summary>A word with at least this many failed reviews counts as "struggling".</summary>
    public const int StrugglingWordFailThreshold = 2;

    /// <summary>This many struggling words means SRS failures are piling up (level too hard).</summary>
    public const int RisingSrsStrugglingWordCount = 3;

    /// <summary>A streak counts as "recently active" if a goal was met within this many days.</summary>
    public const int RecentStreakDays = 3;

    private readonly ILearnerProfileRepository _profiles;
    private readonly IGamificationStore _gamification;
    private readonly IVocabularyRepository _vocabulary;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly TimeProvider _clock;

    public EvaluateChurnRiskQueryHandler(
        ILearnerProfileRepository profiles,
        IGamificationStore gamification,
        IVocabularyRepository vocabulary,
        ISubscriptionRepository subscriptions,
        TimeProvider clock)
    {
        _profiles = profiles;
        _gamification = gamification;
        _vocabulary = vocabulary;
        _subscriptions = subscriptions;
        _clock = clock;
    }

    public async Task<ChurnAssessmentDto> Handle(
        EvaluateChurnRiskQuery request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);

        // No profile means the placement test was never finished - onboarding abandoned.
        if (profile is null)
        {
            var notOnboarded = ChurnEvaluator.Evaluate(new ChurnInputs
            {
                DaysSinceLastActivity = 0,
                OnboardingCompleted = false
            });
            return ChurnAssessmentDto.From(request.LearnerId, notOnboarded);
        }

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var completedDays = await _gamification.GetCompletedDaysAsync(request.LearnerId, cancellationToken);
        var words = await _vocabulary.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        var subscription = await _subscriptions.GetByLearnerIdAsync(request.LearnerId, cancellationToken);

        var inputs = new ChurnInputs
        {
            DaysSinceLastActivity = profile.DaysSinceLastActivity(now),
            OnboardingCompleted = true,
            StreakLapsed = StreakLapsed(completedDays, today),
            UnresolvedSpeakingFrustration = profile.HasUnresolvedSpeakingFrustration(now),
            RisingSrsFailures = RisingSrsFailures(words),
            DaysUntilPremiumExpiry = subscription is { } sub && sub.IsPremiumActive(now)
                ? sub.DaysUntilExpiry(now)
                : null
        };

        return ChurnAssessmentDto.From(request.LearnerId, ChurnEvaluator.Evaluate(inputs));
    }

    // Two consecutive missed days (today and yesterday) after the learner had a live streak
    // in the last few days - the I.1 "2 kun ketma-ket streak buzilishi" signal.
    private static bool StreakLapsed(IReadOnlyCollection<DateOnly> completedDays, DateOnly today)
    {
        var missedTodayAndYesterday =
            !completedDays.Contains(today) && !completedDays.Contains(today.AddDays(-1));
        var hadRecentStreak = completedDays.Any(d => d >= today.AddDays(-RecentStreakDays) && d < today);
        return missedTodayAndYesterday && hadRecentStreak;
    }

    private static bool RisingSrsFailures(IReadOnlyList<Domain.Vocabulary.VocabularyItem> words) =>
        words.Count(w => w.Schedule.FailCount >= StrugglingWordFailThreshold) >= RisingSrsStrugglingWordCount;
}
