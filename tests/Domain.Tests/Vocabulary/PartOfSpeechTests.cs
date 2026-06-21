using Domain.Assessment;
using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Vocabulary;

public class PartOfSpeechParserTests
{
    [Theory]
    [InlineData("noun", PartOfSpeech.Noun)]
    [InlineData("verb", PartOfSpeech.Verb)]
    [InlineData("adjective", PartOfSpeech.Adjective)]
    [InlineData("adverb", PartOfSpeech.Adverb)]
    [InlineData("preposition", PartOfSpeech.Preposition)]
    [InlineData("conjunction", PartOfSpeech.Conjunction)]
    [InlineData("pronoun", PartOfSpeech.Pronoun)]
    [InlineData("determiner", PartOfSpeech.Determiner)]
    [InlineData("article", PartOfSpeech.Determiner)]
    [InlineData("phrasal verb", PartOfSpeech.PhrasalVerb)]
    [InlineData("phrasal-verb", PartOfSpeech.PhrasalVerb)]
    [InlineData("phrasal_verb", PartOfSpeech.PhrasalVerb)]
    [InlineData("collocation", PartOfSpeech.Collocation)]
    [InlineData("idiom", PartOfSpeech.Idiom)]
    [InlineData("expression", PartOfSpeech.Expression)]
    [InlineData("ADJ", PartOfSpeech.Adjective)]
    public void Parse_maps_known_tags(string tag, PartOfSpeech expected) =>
        PartOfSpeechParser.Parse(tag).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("gerund")]
    public void Parse_unknown_or_missing_becomes_Other(string? tag) =>
        PartOfSpeechParser.Parse(tag).Should().Be(PartOfSpeech.Other);
}

public class TopicWordPlanTests
{
    [Theory]
    [InlineData(CefrLevel.A1)]
    [InlineData(CefrLevel.A2)]
    [InlineData(CefrLevel.B1)]
    [InlineData(CefrLevel.B2)]
    [InlineData(CefrLevel.C1)]
    [InlineData(CefrLevel.C2)]
    public void For_never_allocates_more_than_the_requested_total(CefrLevel level)
    {
        var quotas = TopicWordPlan.For(level, VocabularyTopic.TargetWordCount);

        quotas.Sum(q => q.Count).Should().BeLessThanOrEqualTo(VocabularyTopic.TargetWordCount);
        quotas.Should().OnlyContain(q => q.Count > 0);
    }

    [Theory]
    [InlineData(CefrLevel.A1)]
    [InlineData(CefrLevel.A2)]
    public void Beginner_levels_teach_single_words_not_idioms_or_phrasal_verbs(CefrLevel level)
    {
        var categories = TopicWordPlan.For(level, 15).Select(q => q.Category).ToList();

        categories.Should().Contain(PartOfSpeech.Noun);
        categories.Should().Contain(PartOfSpeech.Verb);
        categories.Should().NotContain(PartOfSpeech.Idiom);
        categories.Should().NotContain(PartOfSpeech.PhrasalVerb);
        categories.Should().NotContain(PartOfSpeech.Expression);
    }

    [Fact]
    public void B_levels_introduce_multi_word_chunks()
    {
        var categories = TopicWordPlan.For(CefrLevel.B1, 15).Select(q => q.Category).ToList();

        categories.Should().Contain(PartOfSpeech.PhrasalVerb);
        categories.Should().Contain(PartOfSpeech.Collocation);
    }

    [Fact]
    public void Advanced_levels_teach_idioms_and_expressions()
    {
        var categories = TopicWordPlan.For(CefrLevel.C1, 15).Select(q => q.Category).ToList();

        categories.Should().Contain(PartOfSpeech.Idiom);
        categories.Should().Contain(PartOfSpeech.Expression);
    }
}
