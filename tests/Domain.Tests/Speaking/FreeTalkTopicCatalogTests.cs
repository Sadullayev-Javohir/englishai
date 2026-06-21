using Domain.Assessment;
using Domain.Speaking;
using FluentAssertions;

namespace Domain.Tests.Speaking;

public class FreeTalkTopicCatalogTests
{
    [Fact]
    public void Catalog_has_120_topics_in_total()
    {
        FreeTalkTopicCatalog.All.Should().HaveCount(120);
        FreeTalkTopicCatalog.TotalTopics.Should().Be(120);
    }

    [Theory]
    [InlineData(CefrLevel.A1)]
    [InlineData(CefrLevel.A2)]
    [InlineData(CefrLevel.B1)]
    [InlineData(CefrLevel.B2)]
    [InlineData(CefrLevel.C1)]
    [InlineData(CefrLevel.C2)]
    public void Every_level_has_exactly_20_topics(CefrLevel level)
    {
        var topics = FreeTalkTopicCatalog.ForLevel(level);

        topics.Should().HaveCount(20);
        topics.Should().OnlyContain(t => t.Level == level);
    }

    [Fact]
    public void Topic_codes_are_unique_across_the_whole_catalog()
    {
        var codes = FreeTalkTopicCatalog.All.Select(t => t.Code).ToList();

        codes.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Every_topic_has_a_snake_case_code_and_an_english_title()
    {
        foreach (var topic in FreeTalkTopicCatalog.All)
        {
            topic.Code.Should().NotBeNullOrWhiteSpace();
            topic.Code.Should().MatchRegex("^[a-z0-9_]+$",
                "topic codes are stable snake_case identifiers used to key content-store labels");
            // Codes are sent through the StartConversation validator, which caps the topic at 40 chars.
            topic.Code.Length.Should().BeLessThanOrEqualTo(40);
            topic.EnglishTitle.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void All_is_ordered_by_level_ascending()
    {
        var levels = FreeTalkTopicCatalog.All.Select(t => (int)t.Level).ToList();

        levels.Should().BeInAscendingOrder();
    }

    [Fact]
    public void ForLevel_preserves_curated_order_and_starts_with_the_expected_first_topic()
    {
        FreeTalkTopicCatalog.ForLevel(CefrLevel.A1).First().Code.Should().Be("my_family");
    }
}
