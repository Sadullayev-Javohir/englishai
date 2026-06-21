using Domain.Common;
using Domain.Identity;
using FluentAssertions;

namespace Domain.Tests.Identity;

public sealed class UserAccountDemographicsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Sets_valid_demographics_and_clears_other_text_for_known_source()
    {
        var account = UserAccount.Register("sub", "user@example.com", "User", null, Now);
        account.SetDemographics(new DateOnly(2000, 2, 29), Gender.Female, AcquisitionSource.Google, "ignored", Now);
        account.HasCompletedDemographics.Should().BeTrue();
        account.AcquisitionSourceOther.Should().BeNull();
    }

    [Fact]
    public void Requires_other_source_text()
    {
        var account = UserAccount.Register("sub", "user@example.com", "User", null, Now);
        var action = () => account.SetDemographics(new DateOnly(2000, 1, 1), Gender.Male, AcquisitionSource.Other, " ", Now);
        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rejects_future_and_over_120_birth_dates()
    {
        var account = UserAccount.Register("sub", "user@example.com", "User", null, Now);
        var future = () => account.SetDemographics(new DateOnly(2026, 8, 9), Gender.Male, AcquisitionSource.Telegram, null, Now);
        var old = () => account.SetDemographics(new DateOnly(1905, 8, 7), Gender.Male, AcquisitionSource.Telegram, null, Now);
        future.Should().Throw<DomainException>();
        old.Should().Throw<DomainException>();
    }
}
