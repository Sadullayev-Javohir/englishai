using FluentAssertions;
using Infrastructure.Speaking;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// Per-turn feedback is the app's most repeated Uzbek text. One fixed sentence after every utterance
/// is what makes AI feedback read like a broken machine, so codes may be backed by numbered
/// variants - and the choice has to be stable, or a learner is told the same mistake three
/// different ways.
/// </summary>
public class FeedbackTemplateVariantTests
{
    private static JsonFeedbackTemplateProvider Provider() =>
        JsonFeedbackTemplateProvider.FromEmbeddedResource();

    [Fact]
    public void A_code_backed_by_variants_still_resolves()
    {
        var feedback = Provider().Get(
            "pron.mispronunciation", new Dictionary<string, string> { ["word"] = "think" });

        feedback.Should().NotBeNullOrWhiteSpace();
        feedback.Should().Contain("think", "the word must be filled into the template");
        feedback.Should().NotContain("{word}");
    }

    [Fact]
    public void The_same_word_always_gets_the_same_wording()
    {
        // Deterministic, not random: a learner correcting one word should not see it described
        // differently on every attempt, and the tests must be reproducible.
        var provider = Provider();
        var args = new Dictionary<string, string> { ["word"] = "through" };

        var first = provider.Get("pron.mispronunciation", args);
        var second = provider.Get("pron.mispronunciation", args);

        second.Should().Be(first);
    }

    [Fact]
    public void Different_words_can_get_different_wordings()
    {
        var provider = Provider();

        var phrasings = new[] { "think", "world", "water", "school", "people", "because" }
            .Select(word => provider.Get(
                "pron.mispronunciation", new Dictionary<string, string> { ["word"] = word })!
                .Replace(word, "{word}"))
            .Distinct()
            .ToList();

        phrasings.Should().HaveCountGreaterThan(1, "the feedback must not be one fixed sentence");
    }

    [Fact]
    public void A_variant_code_resolves_without_arguments()
    {
        Provider().Get("pron.good").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void An_unknown_code_is_still_null()
    {
        // The dispatcher falls back to the raw code, so an unknown one must not silently resolve to
        // some other feature's message.
        Provider().Get("pron.definitely_not_a_code").Should().BeNull();
    }

    [Theory]
    [InlineData("tip.th")]
    [InlineData("tip.ng")]
    [InlineData("tip.schwa")]
    [InlineData("tip.ending_ed")]
    [InlineData("roleplay.summary.good")]
    [InlineData("pron.not_recognized")]
    public void Every_referenced_code_has_uzbek_text(string code)
    {
        Provider().Get(code).Should().NotBeNullOrWhiteSpace();
    }
}
