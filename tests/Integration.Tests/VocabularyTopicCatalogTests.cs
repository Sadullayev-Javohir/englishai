using Application.Vocabulary.Models;
using Domain.Assessment;
using Domain.Vocabulary;
using Infrastructure.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Verifies the curated topic catalog - the shared learning spine - meets the scale requirement
/// (50 distinct topics per CEFR level, the same set every skill teaches) and that its grammar
/// focus codes deterministically cover each level's syllabus, plus that the offline passage
/// generator produces quiz-ready content.
/// </summary>
public class VocabularyTopicCatalogTests
{
    private static readonly CefrLevel[] Levels =
        { CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2 };

    [Fact]
    public void Catalog_has_50_distinct_topics_for_every_level()
    {
        var topics = VocabularyTopicCatalog.Topics();

        topics.Should().HaveCount(VocabularyTopicCatalog.TopicsPerLevel * Levels.Length);

        foreach (var level in Levels)
        {
            var forLevel = topics.Where(t => t.Level == level).ToList();
            forLevel.Should().HaveCount(
                VocabularyTopicCatalog.TopicsPerLevel, "level {0} must have 50 topics", level);
            forLevel.Select(t => t.Title).Should().OnlyHaveUniqueItems("titles within a level must be distinct");
        }
    }

    [Fact]
    public void Catalog_slugs_are_globally_unique_and_titles_localised()
    {
        var topics = VocabularyTopicCatalog.Topics();

        topics.Select(t => t.Slug).Should().OnlyHaveUniqueItems();
        topics.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.TitleUz));
        topics.Should().OnlyContain(t => t.Status == VocabularyTopicStatus.Pending);
    }

    [Fact]
    public void Every_topic_carries_a_grammar_focus_drawn_from_its_level_syllabus()
    {
        var topics = VocabularyTopicCatalog.Topics();

        topics.Should().OnlyContain(t => !string.IsNullOrWhiteSpace(t.GrammarFocusCode));

        // Each level cycles a 10-code syllabus across its 50 topics, so every grammar point is
        // taught in exactly five topic contexts - deterministic, complete coverage.
        foreach (var level in Levels)
        {
            var byFocus = topics
                .Where(t => t.Level == level)
                .GroupBy(t => t.GrammarFocusCode)
                .ToList();

            byFocus.Should().HaveCount(10, "level {0} should cover ten grammar points", level);
            byFocus.Should().OnlyContain(g => g.Count() == 5, "each grammar point should appear five times");
        }
    }

    [Fact]
    public void Sequence_orders_each_level_by_theme_category()
    {
        var topics = VocabularyTopicCatalog.Topics();

        foreach (var level in Levels)
        {
            var ordered = topics
                .Where(t => t.Level == level)
                .OrderBy(t => t.Sequence)
                .ToList();

            // Sequence is a contiguous 0..49 ordering per level.
            ordered.Select(t => t.Sequence).Should().Equal(
                Enumerable.Range(0, VocabularyTopicCatalog.TopicsPerLevel),
                "level {0} sequence must be a contiguous 0-based progression", level);

            // Reading the level in sequence order groups the topics category by category: the first
            // five belong to one theme, the next five to the next, and so on. This is the same order
            // the skill catalogs group by, so the /levels roadmap reads identically (one coherent
            // course-like spine across every module).
            var categoryBlocks = ordered
                .Chunk(VocabularyTopicCatalog.TopicsPerCategory)
                .Select(block => block.Select(t => t.Category).Distinct().Count());

            categoryBlocks.Should().OnlyContain(
                distinctCategories => distinctCategories == 1,
                "consecutive blocks of five topics should each stay within a single theme category");

            // Each category block still teaches five distinct grammar points (the syllabus cycles),
            // so grammar coverage stays complete even though it no longer drives the order.
            var focusesPerBlock = ordered
                .Chunk(VocabularyTopicCatalog.TopicsPerCategory)
                .Select(block => block.Select(t => t.GrammarFocusCode).Distinct().Count());

            focusesPerBlock.Should().OnlyContain(
                distinctFocuses => distinctFocuses == VocabularyTopicCatalog.TopicsPerCategory,
                "each block of five topics should cycle through five distinct grammar points");
        }
    }

    [Fact]
    public async Task LocalGenerator_produces_quiz_ready_words_for_each_level()
    {
        var generator = new LocalVocabularyPassageGenerator();

        foreach (var level in Levels)
        {
            var content = await generator.GenerateAsync(
                "Saving Water", level, VocabularyTopic.TargetWordCount, CancellationToken.None);

            content.HasContent.Should().BeTrue();
            content.Words.Should().HaveCount(VocabularyTopic.TargetWordCount);
            content.Words.Should().OnlyContain(w =>
                content.Passage.Contains(w.Word) &&
                w.ExampleSentence != null &&
                w.ExampleSentence.Contains(w.Word));
        }
    }

    [Fact]
    public void ClaudeParser_reads_valid_json_and_rejects_garbage()
    {
        const string json =
            """
            Here you go: {"passage":"We must reduce water use.","words":[
              {"w":"reduce","uz":"kamaytirmoq","ex":"We must reduce water use."}]}
            """;

        var parsed = LlmVocabularyPassageGenerator.Parse(json);
        parsed.HasContent.Should().BeTrue();
        parsed.Words.Should().ContainSingle(w => w.Word == "reduce" && w.Translation == "kamaytirmoq");

        LlmVocabularyPassageGenerator.Parse("not json at all").Should().Be(GeneratedTopicContent.Empty);
        LlmVocabularyPassageGenerator.Parse("{\"words\":[]}").HasContent.Should().BeFalse();
    }

    [Fact]
    public void ClaudeParser_reads_part_of_speech_tag()
    {
        const string json =
            """
            {"passage":"We must reduce water use every day.","words":[
              {"w":"reduce","pos":"verb","uz":"kamaytirmoq","ex":"We must reduce water use every day."},
              {"w":"water","pos":"noun","uz":"suv","ex":"We must reduce water use every day."}]}
            """;

        var parsed = LlmVocabularyPassageGenerator.Parse(json);

        parsed.Words.Single(w => w.Word == "reduce").Pos.Should().Be("verb");
        PartOfSpeechParser.Parse(parsed.Words.Single(w => w.Word == "reduce").Pos)
            .Should().Be(PartOfSpeech.Verb);
        PartOfSpeechParser.Parse(parsed.Words.Single(w => w.Word == "water").Pos)
            .Should().Be(PartOfSpeech.Noun);
        // The 12-category parser recognises "preposition" as a lexical class of its own.
        PartOfSpeechParser.Parse("preposition").Should().Be(PartOfSpeech.Preposition);
        // A missing/unknown tag degrades to Other rather than failing.
        PartOfSpeechParser.Parse(null).Should().Be(PartOfSpeech.Other);
        PartOfSpeechParser.Parse("not-a-real-tag").Should().Be(PartOfSpeech.Other);
    }
}
