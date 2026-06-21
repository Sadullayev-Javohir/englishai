using Application.Grammar.Content;
using Application.Grammar.Dtos;
using Domain.Assessment;
using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Grammar;

public class CuratedGrammarLessonCatalogTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    // Every grammar focus of the learning-spine syllabus - all 10 per CEFR level, 60 in total. The
    // catalog now authors a vetted Uzbek lesson for each one (mirrors VocabularyTopicCatalog's
    // per-level grammar arrays). If a code is added to the spine, add it here too.
    public static readonly string[] AllSpineFocusCodes =
    {
        // A1
        "to-be", "present-simple", "articles", "plural-nouns", "there-is-there-are",
        "possessive-adjectives", "prepositions-of-place", "can-ability", "present-continuous", "wh-questions",
        // A2
        "past-simple", "comparatives", "superlatives", "going-to-future", "adverbs-of-frequency",
        "countable-uncountable", "prepositions-of-time", "object-pronouns", "possessive-pronouns", "imperatives",
        // B1
        "present-perfect", "past-continuous", "first-conditional", "second-conditional", "gerunds-and-infinitives",
        "will-future", "modals-of-obligation", "defining-relative-clauses", "used-to", "comparative-adverbs",
        // B2
        "present-perfect-continuous", "past-perfect", "third-conditional", "passive-voice", "reported-speech",
        "modals-of-deduction", "non-defining-relative-clauses", "future-continuous", "wish-clauses", "causative-have",
        // C1
        "mixed-conditionals", "inversion", "cleft-sentences", "participle-clauses", "advanced-passive",
        "subjunctive", "future-perfect", "ellipsis-and-substitution", "nominalisation", "discourse-markers",
        // C2
        "hypothetical-meaning", "advanced-inversion", "fronting-and-emphasis", "cohesion-devices", "hedging-language",
        "emphatic-structures", "complex-reporting", "concessive-clauses", "idiomatic-modality", "register-and-formality",
    };

    public static IEnumerable<object[]> EverySpineFocus() =>
        AllSpineFocusCodes.Select(code => new object[] { code });

    [Theory]
    [MemberData(nameof(EverySpineFocus))]
    public void Every_spine_focus_has_a_complete_vetted_lesson(string focusCode)
    {
        var lesson = CuratedGrammarLessonCatalog.TryGet(focusCode);

        lesson.Should().NotBeNull($"the spine grammar focus '{focusCode}' should have a curated lesson");
        lesson!.FocusCode.Should().Be(focusCode);
        lesson.TitleUz.Should().NotBeNullOrWhiteSpace();
        lesson.SummaryUz.Should().NotBeNullOrWhiteSpace();
        lesson.Formulas.Should().NotBeEmpty().And.OnlyContain(f => !string.IsNullOrWhiteSpace(f));
        lesson.Rules.Should().NotBeEmpty()
            .And.OnlyContain(r => !string.IsNullOrWhiteSpace(r.HeadingUz) && !string.IsNullOrWhiteSpace(r.BodyUz));
        lesson.Examples.Should().NotBeEmpty()
            .And.OnlyContain(e => !string.IsNullOrWhiteSpace(e.English) && !string.IsNullOrWhiteSpace(e.Uzbek));
        lesson.CommonMistakesUz.Should().NotBeEmpty().And.OnlyContain(m => !string.IsNullOrWhiteSpace(m));
    }

    [Fact]
    public void All_sixty_spine_focuses_are_covered()
    {
        AllSpineFocusCodes.Should().HaveCount(60).And.OnlyHaveUniqueItems();
        AllSpineFocusCodes.Should().OnlyContain(code => CuratedGrammarLessonCatalog.TryGet(code) != null);
    }

    [Fact]
    public void TryGet_is_case_insensitive_and_trims()
    {
        CuratedGrammarLessonCatalog.TryGet("  Past-Simple ")!.FocusCode.Should().Be("past-simple");
    }

    [Theory]
    [InlineData("not-a-real-focus")]   // a code outside the spine - graceful fallback to generated English
    [InlineData("")]
    [InlineData(null)]
    public void TryGet_returns_null_when_no_curated_lesson_exists(string? focusCode)
    {
        CuratedGrammarLessonCatalog.TryGet(focusCode).Should().BeNull();
    }

    [Fact]
    public void Pending_lesson_attaches_the_curated_uzbek_lesson_for_a_curated_focus()
    {
        var topic = VocabularyTopic.Curate(
            "a1-my-family", "My Family", "Mening oilam", "family_people", "to-be", CefrLevel.A1, Now);

        var dto = GrammarLessonDto.Pending(topic);

        dto.IsReady.Should().BeFalse();
        dto.Curated.Should().NotBeNull();
        dto.Curated!.TitleUz.Should().Contain("To be");
        dto.Curated.Examples.Should().NotBeEmpty();
    }

    [Fact]
    public void Pending_lesson_has_no_curated_content_for_an_unauthored_focus()
    {
        var topic = VocabularyTopic.Curate(
            "a1-the-park", "The Park", "Bog'", "places_town", "not-a-real-focus", CefrLevel.A1, Now);

        GrammarLessonDto.Pending(topic).Curated.Should().BeNull();
    }
}
