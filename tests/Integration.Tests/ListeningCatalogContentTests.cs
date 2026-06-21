using Domain.Assessment;
using Infrastructure.Listening;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Guards the curated listening catalog (PROJECT-SPEC Faza 4): it must cover the CEFR ladder
/// with enough leveled exercises that the level-tolerant catalog always offers real choice,
/// each exercise must be a complete comprehension lesson (transcript + questions), and every
/// quiz hint code must resolve to vetted Uzbek content (docs/development-guide.md rule 11).
/// </summary>
public class ListeningCatalogContentTests
{
    private static readonly CefrLevel[] CoreLevels =
        { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 };

    [Fact]
    public void Catalog_covers_every_core_level_with_multiple_exercises()
    {
        var exercises = ListeningCatalogSeed.Exercises();

        exercises.Select(e => e.Title).Should().OnlyHaveUniqueItems();

        // The full ladder must offer a meaningful, progressively harder list (PROJECT-SPEC Faza 4).
        exercises.Should().HaveCountGreaterThanOrEqualTo(7, "the catalog should offer at least seven exercises");

        foreach (var level in CoreLevels)
        {
            exercises.Where(e => e.Level == level).Should()
                .HaveCountGreaterThanOrEqualTo(2, "level {0} needs at least two listening exercises", level);
        }
    }

    [Fact]
    public void Every_exercise_is_a_complete_comprehension_lesson()
    {
        var exercises = ListeningCatalogSeed.Exercises();

        foreach (var exercise in exercises)
        {
            exercise.Transcript.Should().NotBeNullOrWhiteSpace();
            exercise.Questions.Should().HaveCountGreaterThanOrEqualTo(2, "exercise '{0}'", exercise.Title);
        }
    }

    [Fact]
    public void Every_quiz_hint_code_resolves_to_vetted_uzbek_content()
    {
        var exercises = ListeningCatalogSeed.Exercises();
        var content = JsonListeningContentProvider.FromEmbeddedResource();

        foreach (var exercise in exercises)
        {
            foreach (var question in exercise.Questions.Where(q => q.HintCode is not null))
            {
                content.GetHint(question.HintCode).Should()
                    .NotBeNullOrWhiteSpace("hint code '{0}' (exercise '{1}') must be vetted",
                        question.HintCode, exercise.Title);
            }
        }
    }
}
