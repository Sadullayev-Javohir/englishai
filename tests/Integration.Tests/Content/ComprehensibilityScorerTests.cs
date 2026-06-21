using Domain.Content;
using FluentAssertions;
using Infrastructure.Content;
using Xunit;

namespace Integration.Tests.Content;

public sealed class ComprehensibilityScorerTests
{
    [Fact]
    public void Score_counts_inflected_known_lemmas_and_topic_words()
    {
        IComprehensibilityScorer scorer = new ComprehensibilityScorer();

        var result = scorer.Score(
            "She went to markets and bought kumquats.",
            new[] { "she", "go", "to", "market", "and", "buy" },
            new[] { "kumquat" });

        result.TotalTokens.Should().Be(7);
        result.ComprehensibleTokens.Should().Be(7);
        result.Score.Should().Be(100);
    }

    [Fact]
    public void Score_reports_unknown_lemmas_and_uses_token_occurrences()
    {
        var scorer = new ComprehensibilityScorer();

        var result = scorer.Score("Known mystery mystery", new[] { "known" }, Array.Empty<string>());

        result.Score.Should().BeApproximately(33.3, 0.1);
        result.UnknownLemmas.Should().BeEquivalentTo("mystery");
    }
}