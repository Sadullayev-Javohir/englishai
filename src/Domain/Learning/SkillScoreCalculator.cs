namespace Domain.Learning;

/// <summary>
/// Pure computation of a skill's current 0-100 score from its recent activities,
/// implementing the PROJECT-SPEC G.4 rule: "based on the last 30 days of activity,
/// with older results weighted down (exponential decay)".
///
/// A more recent activity counts more: its weight halves every
/// <see cref="HalfLifeDays"/> days. Activities older than <see cref="WindowDays"/>
/// are dropped entirely. When no activity falls inside the window, the placement
/// seed score is used as the baseline so a fresh or idle learner still has a score.
/// </summary>
public static class SkillScoreCalculator
{
    /// <summary>Only activity from the last 30 days contributes (PROJECT-SPEC G.4).</summary>
    public const int WindowDays = 30;

    /// <summary>An activity's weight halves every 14 days, so recent work dominates.</summary>
    public const double HalfLifeDays = 14.0;

    public static SkillScore Compute(
        SkillType skill,
        IEnumerable<SkillActivity> activities,
        double seedScore,
        DateTimeOffset now)
    {
        double weightedSum = 0;
        double totalWeight = 0;
        var sampleCount = 0;

        foreach (var activity in activities)
        {
            if (activity.Skill != skill)
                continue;

            var ageDays = (now - activity.OccurredAt).TotalDays;
            if (ageDays > WindowDays)
                continue;

            // Clamp future-dated or just-recorded activity to age 0 (full weight).
            if (ageDays < 0)
                ageDays = 0;

            var weight = Math.Pow(2, -ageDays / HalfLifeDays);
            weightedSum += weight * activity.Score;
            totalWeight += weight;
            sampleCount++;
        }

        var score = sampleCount == 0 ? seedScore : weightedSum / totalWeight;
        return new SkillScore(skill, Math.Round(score, 1), sampleCount);
    }
}
