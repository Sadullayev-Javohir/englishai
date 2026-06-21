using Domain.Referral;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Referral;

/// <summary>Unit tests for <see cref="ReferralCode"/> generation, normalization and validation.</summary>
public class ReferralCodeTests
{
    [Fact]
    public void Generated_codes_are_valid_and_the_expected_length()
    {
        for (var i = 0; i < 100; i++)
        {
            var code = ReferralCode.Generate();
            code.Should().HaveLength(ReferralCode.Length);
            ReferralCode.IsValid(code).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("  ab12cd  ", "AB12CD")]
    [InlineData("abcdef", "ABCDEF")]
    public void Normalize_trims_and_uppercases(string input, string expected) =>
        ReferralCode.Normalize(input).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]        // too short
    [InlineData("ABCDEFG")]    // too long
    [InlineData("ABCDE0")]     // contains excluded '0'
    [InlineData("ABCDE1")]     // contains excluded '1'
    [InlineData("ABC-DE")]     // punctuation
    public void Invalid_codes_are_rejected(string code) =>
        ReferralCode.IsValid(ReferralCode.Normalize(code)).Should().BeFalse();
}
