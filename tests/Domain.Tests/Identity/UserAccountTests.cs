using Domain.Common;
using Domain.Identity;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Identity;

public class UserAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_sets_identity_and_login_timestamps()
    {
        var account = UserAccount.Register("google-sub-1", "user@example.com", "Aziz Karimov", "https://pic", Now);

        account.Id.Should().NotBe(Guid.Empty);
        account.GoogleSubject.Should().Be("google-sub-1");
        account.Email.Should().Be("user@example.com");
        account.DisplayName.Should().Be("Aziz Karimov");
        account.PictureUrl.Should().Be("https://pic");
        account.CreatedAt.Should().Be(Now);
        account.LastLoginAt.Should().Be(Now);
        account.ProTrialExpiresAt.Should().Be(Now.AddDays(UserAccount.ProTrialDurationDays));
    }

    [Fact]
    public void Pro_trial_runs_for_its_configured_length_and_expires_at_the_boundary()
    {
        var account = UserAccount.Register("sub", "user@example.com", "Aziz Karimov", null, Now);

        account.IsProTrialActive(Now.AddDays(UserAccount.ProTrialDurationDays).AddHours(-1))
            .Should().BeTrue();
        account.IsProTrialActive(Now.AddDays(UserAccount.ProTrialDurationDays)).Should().BeFalse();
    }

    [Fact]
    public void Shortening_the_trial_does_not_move_an_existing_account_expiry()
    {
        // The length is applied at registration, so the constant can change without a migration and
        // without silently cutting short the learners who already have a longer trial recorded.
        var account = UserAccount.Register("sub", "user@example.com", "Aziz Karimov", null, Now);

        account.ProTrialExpiresAt.Should().Be(Now.AddDays(UserAccount.ProTrialDurationDays));
    }

    [Fact]
    public void PreferredName_is_null_until_the_learner_sets_one()
    {
        var account = UserAccount.Register("sub", "user@example.com", "Aziz Karimov", null, Now);

        account.PreferredName.Should().BeNull();
    }

    [Fact]
    public void SetPreferredName_trims_and_stores_the_name()
    {
        var account = UserAccount.Register("sub", "user@example.com", "Aziz Karimov", null, Now);

        account.SetPreferredName("  Aziz  ");

        account.PreferredName.Should().Be("Aziz");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetPreferredName_clears_the_name_when_blank(string? value)
    {
        var account = UserAccount.Register("sub", "user@example.com", "Aziz Karimov", null, Now);
        account.SetPreferredName("Aziz");

        account.SetPreferredName(value);

        account.PreferredName.Should().BeNull();
    }

    [Fact]
    public void SetPreferredName_rejects_a_name_over_the_max_length()
    {
        var account = UserAccount.Register("sub", "user@example.com", "Aziz Karimov", null, Now);

        var act = () => account.SetPreferredName(new string('a', UserAccount.MaxPreferredNameLength + 1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Register_falls_back_to_email_when_display_name_missing()
    {
        var account = UserAccount.Register("sub", "user@example.com", "", null, Now);

        account.DisplayName.Should().Be("user@example.com");
        account.PictureUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("", "user@example.com")]
    [InlineData("sub", "")]
    public void Register_rejects_missing_subject_or_email(string subject, string email)
    {
        var act = () => UserAccount.Register(subject, email, "Name", null, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordLogin_refreshes_profile_and_advances_last_login()
    {
        var account = UserAccount.Register("sub", "old@example.com", "Old Name", null, Now);
        var later = Now.AddDays(5);

        account.RecordLogin("new@example.com", "New Name", "https://new-pic", later);

        account.Email.Should().Be("new@example.com");
        account.DisplayName.Should().Be("New Name");
        account.PictureUrl.Should().Be("https://new-pic");
        account.LastLoginAt.Should().Be(later);
        account.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void RecordLogin_keeps_a_custom_avatar()
    {
        var account = UserAccount.Register("sub", "user@example.com", "Name", "https://google/pic", Now);
        account.SetCustomPicture(Now.AddMinutes(1));

        account.RecordLogin("user@example.com", "Name", "https://google/new-pic", Now.AddDays(1));

        account.PictureUrl.Should().StartWith("/api/auth/avatar/");
    }

    [Fact]
    public void RestoreExternalPicture_replaces_only_a_custom_avatar()
    {
        var account = UserAccount.Register("sub", "user@example.com", "Name", "https://google/pic", Now);
        account.SetCustomPicture(Now.AddMinutes(1));

        account.RestoreExternalPicture("https://google/pic");

        account.PictureUrl.Should().Be("https://google/pic");
    }
}
