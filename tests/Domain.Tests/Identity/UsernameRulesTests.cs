using Domain.Identity;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Identity;

public class UsernameRulesTests
{
    [Theory]
    [InlineData("aziz")]
    [InlineData("aziz_karimov")]
    [InlineData("aziz.karimov")]
    [InlineData("user123")]
    [InlineData("abc")] // exactly the minimum length
    [InlineData("a_very_long_handle_that_is_30c")] // 30 chars
    public void Valid_handles_are_accepted(string username)
    {
        UsernameRules.IsValid(username).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")] // too short
    [InlineData("this_handle_is_definitely_way_too_long")] // > 30
    [InlineData("with space")]
    [InlineData("emoji😀")]
    [InlineData("hyphen-not-allowed")]
    [InlineData("slash/slash")]
    public void Invalid_handles_are_rejected(string username)
    {
        UsernameRules.IsValid(username).Should().BeFalse();
    }

    [Fact]
    public void Normalize_trims_and_lowercases()
    {
        UsernameRules.Normalize("  Aziz_Karimov  ").Should().Be("aziz_karimov");
    }

    [Fact]
    public void Uppercase_is_valid_because_it_normalizes_to_lowercase()
    {
        UsernameRules.IsValid("AZIZ").Should().BeTrue();
    }
}
