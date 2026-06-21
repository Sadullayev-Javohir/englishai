namespace Domain.Retention;

/// <summary>
/// A single day-N retention figure (PROJECT-SPEC I.4): of the learners old enough to have
/// had the chance to return on day N (<see cref="EligibleLearners"/>), how many did
/// (<see cref="RetainedLearners"/>), and the resulting rate in [0, 1].
/// </summary>
public sealed record RetentionRate(int Day, int EligibleLearners, int RetainedLearners)
{
    public double Rate => EligibleLearners == 0 ? 0d : (double)RetainedLearners / EligibleLearners;
}

/// <summary>
/// The headline retention metrics (PROJECT-SPEC I.4): D1/D7/D30 return rates across the
/// learner cohort. Pure data produced by <see cref="RetentionMetricsCalculator"/>.
/// </summary>
public sealed record RetentionMetrics(int CohortSize, RetentionRate D1, RetentionRate D7, RetentionRate D30);
