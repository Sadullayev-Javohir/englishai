using Application.Ai;

namespace Application.Analytics.Dtos;

/// <summary>
/// The founder (operator) growth dashboard payload (PROJECT-SPEC Faza 7 / Qism I.4). A single
/// read model that answers the questions an accelerator asks: how many people use the product
/// (DAU/WAU/MAU), do they come back (D1/D7/D30 retention), and do they pay (free→premium
/// conversion + MRR). All figures are computed as-of <see cref="AsOf"/> (UTC calendar day).
/// </summary>
public sealed record FounderMetricsDto(
    DateOnly AsOf,
    FounderOverviewDto Overview,
    IReadOnlyList<DailyCountDto> ActiveUsersTrend,
    IReadOnlyList<DailyCountDto> SignupsTrend,
    FounderRetentionDto Retention,
    ConversionFunnelDto Funnel,
    IReadOnlyList<ActivationFunnelStepDto> ActivationFunnel,
    IReadOnlyList<GoalSegmentDto> GoalSegments,
    IReadOnlyList<PlanBreakdownDto> PlanBreakdown,
    DemographicsMetricsDto Demographics)
{
    public VariableCostSnapshot? VariableCosts { get; init; }
    public bool CanManageVariableCostBudget { get; init; }
}

public sealed record DemographicsMetricsDto(
    int Completed,
    int Missing,
    double CompletionRatePct,
    IReadOnlyList<DemographicBreakdownDto> Gender,
    IReadOnlyList<DemographicBreakdownDto> AcquisitionSources,
    IReadOnlyList<DemographicBreakdownDto> AgeGroups);

public sealed record DemographicBreakdownDto(string Label, int Count, double RatePct);

/// <summary>The headline cards.</summary>
public sealed record FounderOverviewDto(
    int TotalUsers,
    int OnboardedUsers,
    int PremiumUsers,
    int FreeUsers,
    double ConversionRatePct,
    int Dau,
    int Wau,
    int Mau,
    double StickinessPct,
    int NewUsersToday,
    int NewUsers7d,
    long MrrUzs,
    long ArppuUzs);

/// <summary>One point on a daily time-series (oldest first).</summary>
public sealed record DailyCountDto(DateOnly Day, int Count);

/// <summary>Classic day-N retention across recent registration cohorts.</summary>
public sealed record FounderRetentionDto(
    RetentionPointDto D1,
    RetentionPointDto D7,
    RetentionPointDto D30);

/// <summary>
/// A single retention bracket: of a cohort old enough to be measured (<see cref="CohortSize"/>),
/// how many were active exactly N days after they registered (<see cref="Retained"/>).
/// </summary>
public sealed record RetentionPointDto(int CohortSize, int Retained, double RatePct);

/// <summary>The registered → onboarded → active → paying funnel.</summary>
public sealed record ConversionFunnelDto(
    int Registered,
    int Onboarded,
    int Active7d,
    int Premium);

/// <summary>
/// One sequential milestone in the activation funnel. Rates are computed against registered users and
/// the immediately previous milestone so the founder dashboard can show both absolute proof and drop-off.
/// </summary>
public sealed record ActivationFunnelStepDto(
    string Code,
    int Count,
    double RateFromRegisteredPct,
    double RateFromPreviousPct);

/// <summary>Active-premium subscriptions grouped by plan, with the monthly-normalized revenue.</summary>
public sealed record PlanBreakdownDto(string Plan, int Count, long MrrUzs);

/// <summary>
/// One onboarding-goal segment (goal-based onboarding). Answers the accelerator question "which
/// learner segment should we target first?" by showing, per goal: how many users chose it, how many
/// pay, the free→premium conversion, day-7 retention across recent cohorts, and the monthly revenue
/// the segment contributes.
/// </summary>
public sealed record GoalSegmentDto(
    string Goal,
    int Users,
    int PremiumUsers,
    double ConversionRatePct,
    RetentionPointDto D7,
    long MrrUzs);
