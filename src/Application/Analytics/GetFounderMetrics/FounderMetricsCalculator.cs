using Application.Analytics.Dtos;
using Domain.Analytics;
using Domain.Common;
using Domain.Identity;
using Domain.Learning;
using Domain.Subscription;

namespace Application.Analytics.GetFounderMetrics;

/// <summary>
/// Pure read-model assembler for the founder growth dashboard. Kept free of I/O (the handler does
/// the reads) so the DAU/retention/conversion math is unit-testable in isolation. All windows are
/// measured against an explicit as-of day so the logic is deterministic (docs/development-guide.md 17.2 - never
/// <c>DateTime.Now</c>).
/// </summary>
public static class FounderMetricsCalculator
{
    /// <summary>Trailing days shown on the DAU / signups trend charts.</summary>
    public const int TrendWindowDays = 30;

    /// <summary>
    /// How far back registration cohorts are considered for retention (and therefore how far back
    /// activity is read). Bounds the query and keeps retention focused on recent, actionable cohorts.
    /// </summary>
    public const int RetentionLookbackDays = 60;

    private const int WeekDays = 7;
    private const int MonthDays = 30;

    public static FounderMetricsDto Build(
        DateOnly today,
        DateTimeOffset now,
        IReadOnlyList<UserAccount> accounts,
        IReadOnlyList<LearnerProfile> profiles,
        IReadOnlyList<Domain.Subscription.Subscription> paidSubscriptions,
        IReadOnlyList<StudyActivityDay> activityDays,
        IReadOnlyList<ProductEvent> productEvents,
        DateOnly? trendFrom = null,
        DateOnly? trendTo = null)
    {
        // Activity indexed two ways: distinct learners per day (for DAU/WAU/MAU + the trend), and the
        // set of active days per learner (for cohort retention lookups).
        var learnersByDay = new Dictionary<DateOnly, HashSet<Guid>>();
        var daysByLearner = new Dictionary<Guid, HashSet<DateOnly>>();
        foreach (var a in activityDays)
        {
            (learnersByDay.TryGetValue(a.Day, out var dayset)
                ? dayset
                : learnersByDay[a.Day] = new HashSet<Guid>()).Add(a.LearnerId);

            (daysByLearner.TryGetValue(a.LearnerId, out var learnerset)
                ? learnerset
                : daysByLearner[a.LearnerId] = new HashSet<DateOnly>()).Add(a.Day);
        }

        var rangeTo = trendTo ?? today;
        var rangeFrom = trendFrom ?? rangeTo.AddDays(-(TrendWindowDays - 1));
        var selectedAccounts = accounts
            .Where(account => DateOnly.FromDateTime(account.CreatedAt.UtcDateTime) <= rangeTo)
            .ToList();
        var selectedProfiles = profiles
            .Where(profile => selectedAccounts.Any(account => account.Id == profile.LearnerId))
            .ToList();
        var selectedAccountIds = selectedAccounts.Select(account => account.Id).ToHashSet();
        var activePremiumSubs = paidSubscriptions
            .Where(s => s.IsPremiumActive(now))
            .ToList();
        var selectedPremiumSubs = activePremiumSubs
            .Where(subscription => selectedAccountIds.Contains(subscription.LearnerId))
            .ToList();

        var dau = DistinctActive(learnersByDay, rangeFrom, rangeTo);
        var wau = DistinctActive(learnersByDay, rangeTo.AddDays(-(WeekDays - 1)), rangeTo);
        var mau = DistinctActive(learnersByDay, rangeTo.AddDays(-(MonthDays - 1)), rangeTo);

        // Registration counts keyed by local calendar day.
        var registrationDays = accounts
            .Select(a => DateOnly.FromDateTime(a.CreatedAt.UtcDateTime))
            .ToList();
        var signupsByDay = registrationDays
            .GroupBy(d => d)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalUsers = selectedAccounts.Count;
        var onboardedUsers = selectedProfiles.Count;

        // Premium = learners whose subscription is entitled right now (Premium or Cancelled-but-not-
        // yet-lapsed). GetPaidAsync only returns paid-state rows, so everyone else is on the free tier.
        var premiumUsers = selectedPremiumSubs.Select(s => s.LearnerId).Distinct().Count();
        var freeUsers = Math.Max(0, totalUsers - premiumUsers);
        var conversionRatePct = Percent(premiumUsers, totalUsers);

        var mrrUzs = selectedPremiumSubs
            .Where(s => s.Plan is not null)
            .Sum(s => MonthlyEquivalentUzs(s.Plan!.Value));
        var arppuUzs = premiumUsers > 0 ? (long)Math.Round((double)mrrUzs / premiumUsers) : 0;

        var overview = new FounderOverviewDto(
            TotalUsers: totalUsers,
            OnboardedUsers: onboardedUsers,
            PremiumUsers: premiumUsers,
            FreeUsers: freeUsers,
            ConversionRatePct: conversionRatePct,
            Dau: dau,
            Wau: wau,
            Mau: mau,
            StickinessPct: Percent(dau, mau),
            NewUsersToday: signupsByDay.GetValueOrDefault(rangeTo),
            NewUsers7d: registrationDays.Count(day => day >= rangeFrom && day <= rangeTo),
            MrrUzs: mrrUzs,
            ArppuUzs: arppuUzs);

        var activeTrend = BuildTrend(rangeFrom, rangeTo, day =>
            learnersByDay.TryGetValue(day, out var s) ? s.Count : 0);
        var signupsTrend = BuildTrend(rangeFrom, rangeTo, day => signupsByDay.GetValueOrDefault(day));

        var retention = new FounderRetentionDto(
            D1: Retention(1, today, accounts, daysByLearner),
            D7: Retention(WeekDays, today, accounts, daysByLearner),
            D30: Retention(MonthDays, today, accounts, daysByLearner));

        var funnel = new ConversionFunnelDto(
            Registered: totalUsers,
            Onboarded: onboardedUsers,
            Active7d: dau,
            Premium: premiumUsers);

        var activationFunnel = BuildActivationFunnel(
            rangeTo, selectedAccounts, selectedProfiles, selectedPremiumSubs,
            activityDays.Where(day => day.Day >= rangeFrom && day.Day <= rangeTo).ToList(),
            productEvents.Where(productEvent =>
                DateOnly.FromDateTime(productEvent.OccurredAt.UtcDateTime) >= rangeFrom &&
                DateOnly.FromDateTime(productEvent.OccurredAt.UtcDateTime) <= rangeTo).ToList());

        var goalSegments = BuildGoalSegments(rangeTo, selectedAccounts, selectedProfiles, selectedPremiumSubs, daysByLearner);

        var planBreakdown = selectedPremiumSubs
            .Where(s => s.Plan is not null)
            .GroupBy(s => s.Plan!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new PlanBreakdownDto(
                Plan: g.Key.ToString(),
                Count: g.Select(s => s.LearnerId).Distinct().Count(),
                MrrUzs: g.Sum(s => MonthlyEquivalentUzs(g.Key))))
            .ToList();
        var demographics = BuildDemographics(rangeTo, selectedAccounts);

        return new FounderMetricsDto(
            AsOf: today,
            Overview: overview,
            ActiveUsersTrend: activeTrend,
            SignupsTrend: signupsTrend,
            Retention: retention,
            Funnel: funnel,
            ActivationFunnel: activationFunnel,
            GoalSegments: goalSegments,
            PlanBreakdown: planBreakdown,
            Demographics: demographics);
    }

    private static DemographicsMetricsDto BuildDemographics(DateOnly asOf, IReadOnlyList<UserAccount> accounts)
    {
        var completed = accounts.Count(account => account.HasCompletedDemographics);
        var completeAccounts = accounts.Where(account => account.HasCompletedDemographics).ToList();
        IReadOnlyList<DemographicBreakdownDto> Breakdown(IEnumerable<(string Label, int Count)> values) => values
            .Select(value => new DemographicBreakdownDto(value.Label, value.Count, Percent(value.Count, completed)))
            .ToList();

        var gender = Breakdown(new[]
        {
            ("Male", completeAccounts.Count(a => a.Gender == Gender.Male)),
            ("Female", completeAccounts.Count(a => a.Gender == Gender.Female)),
        });
        var acquisition = Breakdown(Enum.GetValues<AcquisitionSource>().Select(source =>
            (source.ToString(), completeAccounts.Count(account => account.AcquisitionSource == source))));
        var ages = completeAccounts.Select(account => AgeAt(account.BirthDate!.Value, asOf)).ToList();
        var ageGroups = Breakdown(new[]
        {
            ("0-12", ages.Count(age => age <= 12)),
            ("13-17", ages.Count(age => age is >= 13 and <= 17)),
            ("18-24", ages.Count(age => age is >= 18 and <= 24)),
            ("25-34", ages.Count(age => age is >= 25 and <= 34)),
            ("35-44", ages.Count(age => age is >= 35 and <= 44)),
            ("45+", ages.Count(age => age >= 45)),
        });

        return new DemographicsMetricsDto(
            completed,
            Math.Max(0, accounts.Count - completed),
            Percent(completed, accounts.Count),
            gender,
            acquisition,
            ageGroups);
    }

    private static int AgeAt(DateOnly birthDate, DateOnly asOf)
    {
        var age = asOf.Year - birthDate.Year;
        if (birthDate > asOf.AddYears(-age)) age--;
        return age;
    }

    private static IReadOnlyList<GoalSegmentDto> BuildGoalSegments(
        DateOnly today,
        IReadOnlyList<UserAccount> accounts,
        IReadOnlyList<LearnerProfile> profiles,
        IReadOnlyList<Domain.Subscription.Subscription> activePremiumSubs,
        Dictionary<Guid, HashSet<DateOnly>> daysByLearner)
    {
        // Learners with an entitled premium subscription right now, for per-segment conversion/MRR.
        var premiumByLearner = activePremiumSubs
            .Where(s => s.Plan is not null)
            .GroupBy(s => s.LearnerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // The goal lives on the learner profile now, so resolve each account's goal through its
        // profile; accounts that haven't onboarded yet (no profile) fall into the Unspecified segment.
        var goalByLearner = profiles.ToDictionary(p => p.LearnerId, p => p.LearningGoal);

        // One row per learning goal that at least one account maps to, ordered by the enum so the
        // dashboard is stable. Unspecified is included so "not chosen yet" stays visible.
        return accounts
            .GroupBy(a => goalByLearner.GetValueOrDefault(a.Id, LearningGoal.Unspecified))
            .OrderBy(g => g.Key)
            .Select(group =>
            {
                var segmentAccounts = group.ToList();
                var users = segmentAccounts.Count;
                var premiumUsers = segmentAccounts.Count(a => premiumByLearner.ContainsKey(a.Id));
                var mrrUzs = segmentAccounts
                    .Where(a => premiumByLearner.ContainsKey(a.Id))
                    .SelectMany(a => premiumByLearner[a.Id])
                    .Sum(s => MonthlyEquivalentUzs(s.Plan!.Value));

                return new GoalSegmentDto(
                    Goal: group.Key.ToString(),
                    Users: users,
                    PremiumUsers: premiumUsers,
                    ConversionRatePct: Percent(premiumUsers, users),
                    D7: Retention(WeekDays, today, segmentAccounts, daysByLearner),
                    MrrUzs: mrrUzs);
            })
            .ToList();
    }

    private static IReadOnlyList<ActivationFunnelStepDto> BuildActivationFunnel(
        DateOnly today,
        IReadOnlyList<UserAccount> accounts,
        IReadOnlyList<LearnerProfile> profiles,
        IReadOnlyList<Domain.Subscription.Subscription> activePremiumSubs,
        IReadOnlyList<StudyActivityDay> activityDays,
        IReadOnlyList<ProductEvent> productEvents)
    {
        var registered = accounts.Select(a => a.Id).ToHashSet();
        var current = new HashSet<Guid>(registered);
        var steps = new List<ActivationFunnelStepDto>(capacity: 10);

        void AddStep(string code, IEnumerable<Guid> eligible)
        {
            var eligibleSet = eligible.Where(registered.Contains).ToHashSet();
            current.IntersectWith(eligibleSet);
            var previous = steps.Count == 0 ? registered.Count : steps[^1].Count;
            steps.Add(new ActivationFunnelStepDto(
                code,
                current.Count,
                Percent(current.Count, registered.Count),
                Percent(current.Count, previous)));
        }

        steps.Add(new ActivationFunnelStepDto(
            "registered", registered.Count, Percent(registered.Count, registered.Count), 100));

        var profileLearners = profiles.Select(p => p.LearnerId).ToHashSet();
        var eventLearners = productEvents
            .GroupBy(e => e.Type)
            .ToDictionary(g => g.Key, g => g.Select(e => e.LearnerId).ToHashSet());

        HashSet<Guid> LearnersFor(ProductEventType type) =>
            eventLearners.TryGetValue(type, out var learners) ? learners : new HashSet<Guid>();

        var usernameSet = LearnersFor(ProductEventType.UsernameSetupCompleted);
        AddStep(
            "usernameSetupCompleted",
            registered.Where(id => usernameSet.Contains(id) || accounts.Any(a => a.Id == id && a.Username is not null)));

        var placementStarted = LearnersFor(ProductEventType.PlacementStarted);
        placementStarted.UnionWith(profileLearners); // completed/onboarded implies the placement path was started historically.
        AddStep("placementStarted", placementStarted);

        var placementCompleted = LearnersFor(ProductEventType.PlacementCompleted);
        placementCompleted.UnionWith(profileLearners);
        AddStep("placementCompleted", placementCompleted);

        AddStep("firstTopicOpened", LearnersFor(ProductEventType.TopicOpened));
        AddStep("firstSpeakingSessionCompleted", LearnersFor(ProductEventType.SpeakingSessionCompleted));
        AddStep("firstPronunciationFeedbackViewed", LearnersFor(ProductEventType.PronunciationFeedbackViewed));

        var accountsById = accounts.ToDictionary(a => a.Id);
        var eventsByLearner = productEvents
            .Where(e => e.Type != ProductEventType.Registered)
            .GroupBy(e => e.LearnerId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var activityDaysByLearner = activityDays
            .GroupBy(a => a.LearnerId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.Day).ToHashSet());

        AddStep("day1Returned", registered.Where(id => ReturnedOnDay(
            id, offsetDays: 1, today, accountsById, eventsByLearner, activityDaysByLearner)));

        AddStep("day7Active", registered.Where(id => SpokeThreeTimesInFirstWeek(
            id, today, accountsById, eventsByLearner)));

        var monetization = LearnersFor(ProductEventType.PaywallHit);
        monetization.UnionWith(LearnersFor(ProductEventType.UpgradeClicked));
        monetization.UnionWith(LearnersFor(ProductEventType.PaymentCompleted));
        monetization.UnionWith(activePremiumSubs.Select(s => s.LearnerId));
        AddStep("monetizationIntent", monetization);

        return steps;
    }

    private static bool ReturnedOnDay(
        Guid learnerId,
        int offsetDays,
        DateOnly today,
        Dictionary<Guid, UserAccount> accountsById,
        Dictionary<Guid, List<ProductEvent>> eventsByLearner,
        Dictionary<Guid, HashSet<DateOnly>> activityDaysByLearner)
    {
        if (!accountsById.TryGetValue(learnerId, out var account))
            return false;

        var registered = DateOnly.FromDateTime(account.CreatedAt.UtcDateTime);
        var target = registered.AddDays(offsetDays);
        if (target > today)
            return false;

        if (activityDaysByLearner.TryGetValue(learnerId, out var activeDays) && activeDays.Contains(target))
            return true;

        return eventsByLearner.TryGetValue(learnerId, out var events)
               && events.Any(e => DateOnly.FromDateTime(e.OccurredAt.UtcDateTime) == target);
    }

    private static bool SpokeThreeTimesInFirstWeek(
        Guid learnerId,
        DateOnly today,
        Dictionary<Guid, UserAccount> accountsById,
        Dictionary<Guid, List<ProductEvent>> eventsByLearner)
    {
        if (!accountsById.TryGetValue(learnerId, out var account))
            return false;

        var registered = DateOnly.FromDateTime(account.CreatedAt.UtcDateTime);
        var windowEnd = registered.AddDays(7);
        if (windowEnd > today)
            return false;

        if (!eventsByLearner.TryGetValue(learnerId, out var events))
            return false;

        return events
            .Where(e => e.Type == ProductEventType.SpeakingSessionCompleted)
            .GroupBy(e => e.Source ?? e.Id.ToString(), StringComparer.Ordinal)
            .Count(g =>
            {
                var occurred = DateOnly.FromDateTime(g.Min(e => e.OccurredAt).UtcDateTime);
                return occurred >= registered && occurred <= windowEnd;
            }) >= 3;
    }

    private static int DistinctActive(
        Dictionary<DateOnly, HashSet<Guid>> learnersByDay, DateOnly from, DateOnly to)
    {
        var seen = new HashSet<Guid>();
        for (var day = from; day <= to; day = day.AddDays(1))
            if (learnersByDay.TryGetValue(day, out var set))
                seen.UnionWith(set);
        return seen.Count;
    }

    private static List<DailyCountDto> BuildTrend(
        DateOnly from,
        DateOnly to,
        Func<DateOnly, int> countFor)
    {
        var points = new List<DailyCountDto>(Math.Max(1, to.DayNumber - from.DayNumber + 1));
        for (var day = from; day <= to; day = day.AddDays(1))
            points.Add(new DailyCountDto(day, countFor(day)));
        return points;
    }

    /// <summary>
    /// Classic day-N retention: of the accounts that registered within the look-back window and are
    /// at least N days old (so day N has actually elapsed), how many recorded study time on the exact
    /// calendar day N days after they registered.
    /// </summary>
    private static RetentionPointDto Retention(
        int n,
        DateOnly today,
        IReadOnlyList<UserAccount> accounts,
        Dictionary<Guid, HashSet<DateOnly>> daysByLearner)
    {
        var earliestCohort = today.AddDays(-RetentionLookbackDays);
        var latestMeasurable = today.AddDays(-n);

        var cohortSize = 0;
        var retained = 0;
        foreach (var account in accounts)
        {
            var registered = DateOnly.FromDateTime(account.CreatedAt.UtcDateTime);
            if (registered < earliestCohort || registered > latestMeasurable)
                continue;

            cohortSize++;
            if (daysByLearner.TryGetValue(account.Id, out var days) && days.Contains(registered.AddDays(n)))
                retained++;
        }

        return new RetentionPointDto(cohortSize, retained, Percent(retained, cohortSize));
    }

    /// <summary>Plan price normalized to a 30-day month, so plans of different lengths sum into one MRR.</summary>
    private static long MonthlyEquivalentUzs(SubscriptionPlan plan) =>
        (long)Math.Round(SubscriptionPricing.PriceUzs(plan) * (double)MonthDays / SubscriptionPricing.DurationDays(plan));

    private static double Percent(int part, int whole) =>
        whole <= 0 ? 0 : Math.Round((double)part / whole * 100, 1);
}
