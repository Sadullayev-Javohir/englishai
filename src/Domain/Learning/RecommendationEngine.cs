namespace Domain.Learning;

/// <summary>
/// Pure recommendation logic (PROJECT-SPEC Faza 2 "tavsiya dvigateli"). From the
/// learner's current skill scores, error heatmap and level-up status it produces a
/// short, priority-ordered list of structured suggestions. It deliberately returns
/// codes, not Uzbek text, so wording stays in the content layer (docs/development-guide.md rule 11).
/// </summary>
public static class RecommendationEngine
{
    /// <summary>Recommendation codes, kept here so producers and templates stay in sync.</summary>
    public const string TakeConfirmationTestCode = "recommend.take_confirmation_test";
    public const string FocusSkillCode = "recommend.focus_skill";
    public const string FixErrorCode = "recommend.fix_error";

    private const int MaxRecommendations = 3;

    public static IReadOnlyList<Recommendation> Recommend(
        IReadOnlyCollection<SkillScore> skillScores,
        IReadOnlyDictionary<ErrorCategory, int> errorHeatmap,
        LevelTransitionStatus levelStatus)
    {
        var recommendations = new List<Recommendation>();

        // 1. Closest to levelling up: nudge the learner to take the confirmation test.
        if (levelStatus.IsEligible)
            recommendations.Add(new Recommendation(TakeConfirmationTestCode));

        // 2. Shore up the weakest skill.
        if (skillScores.Count > 0)
        {
            var weakest = skillScores.OrderBy(s => s.Score).ThenBy(s => s.Skill).First();
            recommendations.Add(new Recommendation(FocusSkillCode, Skill: weakest.Skill));
        }

        // 3. Address the most frequent error category.
        if (errorHeatmap.Count > 0)
        {
            var topError = errorHeatmap
                .OrderByDescending(e => e.Value)
                .ThenBy(e => e.Key)
                .First();

            if (topError.Value > 0)
                recommendations.Add(new Recommendation(FixErrorCode, Category: topError.Key));
        }

        return recommendations.Take(MaxRecommendations).ToList();
    }
}
