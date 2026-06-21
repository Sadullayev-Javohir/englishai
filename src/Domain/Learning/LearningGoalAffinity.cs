using Domain.Common;

namespace Domain.Learning;

/// <summary>
/// Maps a <see cref="LearningGoal"/> to the vocabulary-topic theme categories that matter most for
/// that goal. Pure lookup table - no I/O - so it is trivially unit-testable and shared by every
/// recommendation path (home strip, level-map badge, free-talk ordering). It never HIDES topics: it
/// only produces a relevance weight used to bubble goal-relevant topics to the top, so the K.5
/// sequential-lock ordering on the level map is untouched.
///
/// The category codes below must be a subset of the codes emitted by the vocabulary topic catalog
/// (Infrastructure.Vocabulary.VocabularyTopicCatalog). Because the catalog uses different theme
/// codes per CEFR level, each goal lists the relevant codes across all levels; codes that do not
/// occur at a given level simply never match (weight 0). A cross-check test guards this subset
/// invariant against the catalog.
/// </summary>
public static class LearningGoalAffinity
{
    // Ordered from most to least relevant; Weight uses the position so the first category is the
    // heaviest. Codes are the real snake_case catalog categories (A1→C2).
    private static readonly IReadOnlyDictionary<LearningGoal, string[]> Preferred =
        new Dictionary<LearningGoal, string[]>
        {
            [LearningGoal.Work] = new[]
            {
                "work_jobs", "work_career", "work_economy", "business_leadership",
                "money_shopping", "economics_markets", "macroeconomics_finance",
                "media_communication", "technology_internet", "innovation_disruption",
            },
            [LearningGoal.IeltsCefr] = new[]
            {
                "education_knowledge", "education_learning", "science_research", "science_innovation",
                "environment_policy", "environment_sustainability", "sustainability_climate",
                "society_issues", "global_issues", "philosophy_ethics", "linguistics_language",
            },
            [LearningGoal.Migration] = new[]
            {
                "law_justice", "jurisprudence_rights", "work_jobs", "work_career",
                "health_body", "health_lifestyle", "body_health",
                "city_life", "places_town", "town_directions", "money_shopping", "society_issues",
            },
            [LearningGoal.Travel] = new[]
            {
                "travel_transport", "travel_culture", "places_town", "town_directions",
                "food_drink", "food_eating_out", "culture_arts", "weather_seasons",
            },
            [LearningGoal.School] = new[]
            {
                "school", "education_knowledge", "education_learning", "science_research",
                "free_time_toys",
            },
            [LearningGoal.GeneralSpeaking] = new[]
            {
                "family_people", "home_routine", "daily_life", "hobbies_free_time",
                "free_time_toys", "relationships_society", "media_entertainment",
            },
        };

    /// <summary>The catalog theme categories preferred for a goal, most relevant first. Empty for
    /// <see cref="LearningGoal.Unspecified"/> (no tailoring - plain catalog order).</summary>
    public static IReadOnlyList<string> PreferredCategories(LearningGoal goal) =>
        Preferred.TryGetValue(goal, out var categories) ? categories : Array.Empty<string>();

    /// <summary>
    /// How relevant a topic's <paramref name="category"/> is to <paramref name="goal"/>: a positive
    /// weight (highest for the goal's top category) or 0 when it is not a preferred category. Used as
    /// the primary sort key, with the topic's own Sequence as a stable tie-breaker.
    /// </summary>
    public static int Weight(LearningGoal goal, string category)
    {
        var prefs = PreferredCategories(goal);
        for (var i = 0; i < prefs.Count; i++)
            if (string.Equals(prefs[i], category, StringComparison.Ordinal))
                return prefs.Count - i;
        return 0;
    }

    /// <summary>True when the topic's category is one the goal prefers (drives the "🎯" relevance
    /// badge on the level map).</summary>
    public static bool IsRelevant(LearningGoal goal, string category) => Weight(goal, category) > 0;
}
