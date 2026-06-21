using Application.Identity.GetUserPreferences;
using Application.Identity.Ports;
using Application.Identity.UpdateUserPreferences;
using Application.Tests.Learning;
using Domain.Identity;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Identity;

public class UserPreferencesHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private readonly IUserPreferencesStore _store = Substitute.For<IUserPreferencesStore>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    [Fact]
    public async Task Get_returns_defaults_when_nothing_saved()
    {
        _store.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserPreferences?)null);

        var result = await new GetUserPreferencesQueryHandler(_store)
            .Handle(new GetUserPreferencesQuery(Guid.NewGuid()), CancellationToken.None);

        result.DailyGoal.Should().Be(DailyGoal.Normal);
        result.LanguageBalance.Should().Be(LanguageBalance.Bilingual);
        result.EmailNotifications.Should().BeTrue();
        result.PushNotifications.Should().BeTrue();
    }

    [Fact]
    public async Task Get_returns_saved_preferences()
    {
        var userId = Guid.NewGuid();
        var saved = UserPreferences.CreateDefault(userId, Now);
        saved.Update(DailyGoal.Intense, LanguageBalance.English, false, false, Now);
        _store.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(saved);

        var result = await new GetUserPreferencesQueryHandler(_store)
            .Handle(new GetUserPreferencesQuery(userId), CancellationToken.None);

        result.DailyGoal.Should().Be(DailyGoal.Intense);
        result.LanguageBalance.Should().Be(LanguageBalance.English);
        result.EmailNotifications.Should().BeFalse();
    }

    [Fact]
    public async Task Update_creates_on_first_save_and_persists()
    {
        var userId = Guid.NewGuid();
        _store.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserPreferences?)null);

        var result = await new UpdateUserPreferencesCommandHandler(_store, _clock).Handle(
            new UpdateUserPreferencesCommand(userId, DailyGoal.Calm, LanguageBalance.Uzbek, true, false),
            CancellationToken.None);

        result.DailyGoal.Should().Be(DailyGoal.Calm);
        result.LanguageBalance.Should().Be(LanguageBalance.Uzbek);
        result.PushNotifications.Should().BeFalse();
        await _store.Received(1).SaveAsync(
            Arg.Is<UserPreferences>(p => p.UserId == userId && p.DailyGoal == DailyGoal.Calm),
            Arg.Any<CancellationToken>());
    }
}
