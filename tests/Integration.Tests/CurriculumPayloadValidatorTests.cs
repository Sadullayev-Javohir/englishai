using System.Text.Json;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Curriculum;

namespace Integration.Tests;

public sealed class CurriculumPayloadValidatorTests
{
    [Fact]
    public void Lexical_category_parser_supports_all_curriculum_categories()
    {
        var names = new[] { "parts of speech", "phrasal verb", "idiom", "collocation", "expression", "proverb", "slang", "formal expression", "informal expression", "fixed phrase", "sentence starter", "linking word", "discourse marker", "common question", "common response", "greeting", "farewell", "polite expression" };
        names.Select(x => LexicalCategoryParser.Parse(x)).Distinct().Should().HaveCount(18);
    }

    [Fact]
    public void Vocabulary_requires_exactly_twenty_unique_items()
    {
        var words = Enumerable.Range(1, 20).Select(i => new
        {
            w = $"word {i}", uz = $"tarjima {i}", ex = $"I use word {i} today.",
            category = "PartsOfSpeech", pos = "noun", register = "neutral", usageNote = "Oddiy so'z."
        }).ToArray();
        var json = JsonSerializer.Serialize(new
        {
            passage = string.Join(" ", words.Select(x => x.ex)),
            words
        });
        CurriculumPayloadValidator.Validate("vocabulary", json).Approved.Should().BeTrue();
    }

    [Fact]
    public void Validator_rejects_forbidden_scripts()
    {
        var result = CurriculumPayloadValidator.Validate("writing", "{\"prompt\":\"Привет world\"}");
        result.Approved.Should().BeFalse();
        result.Issues.Should().Contain("forbidden_script");
    }
}
