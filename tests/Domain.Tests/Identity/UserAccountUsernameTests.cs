using Domain.Common;
using Domain.Identity;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Identity;

public class UserAccountUsernameTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private static UserAccount NewAccount() =>
        UserAccount.Register("sub-1", "user@example.com", "Aziz", null, Now);

    [Fact]
    public void New_account_has_no_username_until_chosen()
    {
        NewAccount().Username.Should().BeNull();
    }

    [Fact]
    public void SetUsername_stores_the_normalized_lowercase_value()
    {
        var account = NewAccount();

        account.SetUsername("  Aziz_Karimov  ");

        account.Username.Should().Be("aziz_karimov");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("bad*char")]
    public void SetUsername_rejects_invalid_formats(string username)
    {
        var account = NewAccount();

        var act = () => account.SetUsername(username);

        act.Should().Throw<DomainException>();
        account.Username.Should().BeNull();
    }

    [Fact]
    public void UpdateDisplayName_trims_and_sets()
    {
        var account = NewAccount();

        account.UpdateDisplayName("  Yangi Ism  ");

        account.DisplayName.Should().Be("Yangi Ism");
    }

    [Fact]
    public void UpdateDisplayName_rejects_blank()
    {
        var account = NewAccount();

        var act = () => account.UpdateDisplayName("   ");

        act.Should().Throw<DomainException>();
    }
}
