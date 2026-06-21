using Application.Speaking.GetFreeTalkTopics;
using Domain.Assessment;
using Domain.Speaking;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Speaking;

public class GetFreeTalkTopicsQueryHandlerTests
{
    private static readonly GetFreeTalkTopicsQueryHandler Handler = new();

    [Fact]
    public async Task Handle_without_a_level_returns_the_whole_120_topic_catalog()
    {
        var result = await Handler.Handle(new GetFreeTalkTopicsQuery(), CancellationToken.None);

        result.Should().HaveCount(FreeTalkTopicCatalog.TotalTopics);
        result.Select(t => t.Code).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_with_a_level_returns_only_that_levels_20_topics()
    {
        var result = await Handler.Handle(
            new GetFreeTalkTopicsQuery(CefrLevel.B1), CancellationToken.None);

        result.Should().HaveCount(20);
        result.Should().OnlyContain(t => t.Level == CefrLevel.B1);
    }

    [Fact]
    public async Task Handle_maps_domain_topics_faithfully()
    {
        var result = await Handler.Handle(
            new GetFreeTalkTopicsQuery(CefrLevel.A1), CancellationToken.None);

        var first = result[0];
        var expected = FreeTalkTopicCatalog.ForLevel(CefrLevel.A1)[0];
        first.Code.Should().Be(expected.Code);
        first.EnglishTitle.Should().Be(expected.EnglishTitle);
        first.Level.Should().Be(expected.Level);
    }
}
