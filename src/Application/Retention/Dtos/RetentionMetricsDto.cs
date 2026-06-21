using Domain.Retention;

namespace Application.Retention.Dtos;

/// <summary>
/// Headline cohort retention metrics for the growth dashboard (PROJECT-SPEC I.4): D1/D7/D30
/// return rates, each as a percentage plus the eligible/retained counts behind it.
/// </summary>
public sealed record RetentionMetricsDto(
    int CohortSize,
    RetentionRateDto D1,
    RetentionRateDto D7,
    RetentionRateDto D30)
{
    public static RetentionMetricsDto From(RetentionMetrics metrics) =>
        new(
            metrics.CohortSize,
            RetentionRateDto.From(metrics.D1),
            RetentionRateDto.From(metrics.D7),
            RetentionRateDto.From(metrics.D30));
}

public sealed record RetentionRateDto(int Day, int EligibleLearners, int RetainedLearners, double RatePercent)
{
    public static RetentionRateDto From(RetentionRate rate) =>
        new(rate.Day, rate.EligibleLearners, rate.RetainedLearners, Math.Round(rate.Rate * 100, 1));
}
