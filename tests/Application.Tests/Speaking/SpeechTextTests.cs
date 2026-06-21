using Application.Speaking.Common;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Speaking;

public class SpeechTextTests
{
    [Fact]
    public void Clean_removes_markdown_emphasis_markers_but_keeps_words()
    {
        SpeechText.Clean("That is **really** great and _very_ good")
            .Should().Be("That is really great and very good");
    }

    [Fact]
    public void Clean_removes_emojis_and_stickers()
    {
        SpeechText.Clean("Well done 😀🎉 keep going ⭐")
            .Should().Be("Well done keep going");
    }

    [Fact]
    public void Clean_unwraps_markdown_links_to_their_label()
    {
        SpeechText.Clean("See [the guide](https://example.com) now")
            .Should().Be("See the guide now");
    }

    [Fact]
    public void Clean_strips_heading_and_quote_markers()
    {
        SpeechText.Clean("# Title\n> quoted line")
            .Should().Be("Title quoted line");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("🎉👍")]
    public void Clean_returns_empty_for_blank_or_symbol_only_input(string? input)
    {
        SpeechText.Clean(input).Should().BeEmpty();
    }

    [Fact]
    public void Clean_preserves_ordinary_punctuation()
    {
        SpeechText.Clean("Hello, Javohir! How are you?")
            .Should().Be("Hello, Javohir! How are you?");
    }
}
