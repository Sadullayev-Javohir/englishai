using FluentAssertions;
using Infrastructure.Video;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Fixture tests for the LLM translation-response parser (the rule-11 "second check" pass):
/// it extracts per-line Uzbek translations and a deduped word glossary, and yields an empty
/// result for any malformed payload rather than fabricating or throwing.
/// </summary>
public class TranscriptTranslationParserTests
{
    [Fact]
    public void Parses_lines_and_glossary_from_clean_json()
    {
        const string json = """
            {"lines":["Salom.","Darsga xush kelibsiz."],
             "glossary":[{"w":"welcome","uz":"xush kelibsiz"},{"w":"lesson","uz":"dars"}]}
            """;

        var result = TranscriptTranslationParser.Parse(json, expectedLineCount: 2);

        result.LineTranslations.Should().Equal("Salom.", "Darsga xush kelibsiz.");
        result.Glossary.Should().HaveCount(2);
        result.Glossary[0].Word.Should().Be("welcome");
        result.Glossary[0].UzbekMeaning.Should().Be("xush kelibsiz");
    }

    [Fact]
    public void Strips_code_fences_and_prose_around_the_object()
    {
        const string text = "Here is the translation:\n```json\n{\"lines\":[\"Salom.\"],\"glossary\":[]}\n```\nDone.";

        var result = TranscriptTranslationParser.Parse(text, expectedLineCount: 1);

        result.LineTranslations.Should().Equal("Salom.");
        result.Glossary.Should().BeEmpty();
    }

    [Fact]
    public void Pads_with_null_when_fewer_lines_and_truncates_when_more()
    {
        var padded = TranscriptTranslationParser.Parse("{\"lines\":[\"A\"]}", expectedLineCount: 3);
        padded.LineTranslations.Should().Equal("A", null, null);

        var truncated = TranscriptTranslationParser.Parse("{\"lines\":[\"A\",\"B\",\"C\"]}", expectedLineCount: 2);
        truncated.LineTranslations.Should().Equal("A", "B");
    }

    [Fact]
    public void Dedupes_glossary_and_skips_blank_entries()
    {
        const string json = """
            {"lines":[],"glossary":[
              {"w":"run","uz":"yugurmoq"},
              {"w":"Run","uz":"ishga tushirmoq"},
              {"w":"","uz":"x"},
              {"w":"jump","uz":""}]}
            """;

        var result = TranscriptTranslationParser.Parse(json, expectedLineCount: 0);

        result.Glossary.Should().ContainSingle();
        result.Glossary[0].Word.Should().Be("run");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json at all")]
    [InlineData("{ broken")]
    [InlineData(null)]
    public void Returns_empty_for_malformed_or_missing_payloads(string? text)
    {
        var result = TranscriptTranslationParser.Parse(text, expectedLineCount: 2);

        // No fabricated text - an empty result, so the transcript stays honest "pending".
        result.IsEmpty.Should().BeTrue();
    }
}
