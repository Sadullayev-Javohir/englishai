using Domain.Common;
using Domain.Learning;
using FluentAssertions;
using Infrastructure.Vocabulary;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Guards the goal-based onboarding affinity table (Domain) against the real vocabulary topic
/// catalog (Infrastructure): every category a goal claims to prefer must actually be emitted by the
/// catalog, and every real goal must match at least one topic somewhere in the spine. Domain unit
/// tests can't see the catalog (Infrastructure), so this invariant lives here.
/// </summary>
public class LearningGoalCatalogAffinityTests
{
    private static readonly HashSet<string> CatalogCategories =
        VocabularyTopicCatalog.Topics().Select(t => t.Category).ToHashSet(StringComparer.Ordinal);

    public static IEnumerable<object[]> RealGoals() =>
        Enum.GetValues<LearningGoal>()
            .Where(g => g != LearningGoal.Unspecified)
            .Select(g => new object[] { g });

    [Theory]
    [MemberData(nameof(RealGoals))]
    public void Every_preferred_category_exists_in_the_catalog(LearningGoal goal)
    {
        var unknown = LearningGoalAffinity.PreferredCategories(goal)
            .Where(c => !CatalogCategories.Contains(c))
            .ToList();

        unknown.Should().BeEmpty(
            $"goal {goal} references categories that the vocabulary catalog does not emit: {string.Join(", ", unknown)}");
    }

    [Theory]
    [MemberData(nameof(RealGoals))]
    public void Every_real_goal_matches_at_least_one_catalog_topic(LearningGoal goal)
    {
        var matches = VocabularyTopicCatalog.Topics()
            .Count(t => LearningGoalAffinity.IsRelevant(goal, t.Category));

        matches.Should().BeGreaterThan(0, $"goal {goal} should surface at least one topic");
    }
}
