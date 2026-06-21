using Domain.Identity;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Identity;

public class UserPreferencesTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDefault_uses_sensible_defaults()
    {
        var userId = Guid.NewGuid();

        var prefs = UserPreferences.CreateDefault(userId, Now);

        prefs.UserId.Should().Be(userId);
        prefs.DailyGoal.Should().Be(DailyGoal.Normal);
        prefs.LanguageBalance.Should().Be(LanguageBalance.Bilingual);
        prefs.EmailNotifications.Should().BeTrue();
        prefs.PushNotifications.Should().BeTrue();
        prefs.UpdatedAt.Should().Be(Now);
    }

    [Fact]
    public void Update_changes_every_field_and_timestamp()
    {
        var prefs = UserPreferences.CreateDefault(Guid.NewGuid(), Now);
        var later = Now.AddDays(1);

        prefs.Update(DailyGoal.Intense, LanguageBalance.English, emailNotifications: false, pushNotifications: false, later);

        prefs.DailyGoal.Should().Be(DailyGoal.Intense);
        prefs.LanguageBalance.Should().Be(LanguageBalance.English);
        prefs.EmailNotifications.Should().BeFalse();
        prefs.PushNotifications.Should().BeFalse();
        prefs.UpdatedAt.Should().Be(later);
    }
}
