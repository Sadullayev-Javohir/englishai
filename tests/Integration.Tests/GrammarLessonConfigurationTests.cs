using FluentAssertions;
using Infrastructure.Grammar;
using Xunit;

namespace Integration.Tests;

public sealed class GrammarLessonConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\"")]
    [InlineData("{}")]
    public void DeserializeStringList_returns_empty_for_legacy_invalid_values(string? json)
    {
        GrammarLessonConfiguration.DeserializeStringList(json).Should().BeEmpty();
    }

    [Fact]
    public void DeserializeStringList_reads_json_array()
    {
        GrammarLessonConfiguration.DeserializeStringList("[\"Subject + verb\",\"Do + subject\"]")
            .Should().Equal("Subject + verb", "Do + subject");
    }
}
