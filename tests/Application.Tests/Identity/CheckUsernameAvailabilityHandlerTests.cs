using Application.Identity.CheckUsernameAvailability;
using Application.Identity.Ports;
using Domain.Identity;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Identity;

public class CheckUsernameAvailabilityHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();

    private CheckUsernameAvailabilityQueryHandler CreateHandler() => new(_accounts);

    [Fact]
    public async Task Invalid_format_reports_invalid_and_unavailable_without_a_lookup()
    {
        var result = await CreateHandler().Handle(
            new CheckUsernameAvailabilityQuery("ab", Guid.NewGuid()), CancellationToken.None);

        result.IsValidFormat.Should().BeFalse();
        result.IsAvailable.Should().BeFalse();
        await _accounts.DidNotReceive().GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Free_handle_is_available()
    {
        _accounts.GetByUsernameAsync("aziz", Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await CreateHandler().Handle(
            new CheckUsernameAvailabilityQuery("Aziz", Guid.NewGuid()), CancellationToken.None);

        result.IsValidFormat.Should().BeTrue();
        result.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_held_by_someone_else_is_unavailable()
    {
        var other = UserAccount.Register("sub-2", "other@example.com", "Other", null, Now);
        other.SetUsername("aziz");
        _accounts.GetByUsernameAsync("aziz", Arg.Any<CancellationToken>()).Returns(other);

        var result = await CreateHandler().Handle(
            new CheckUsernameAvailabilityQuery("aziz", Guid.NewGuid()), CancellationToken.None);

        result.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task The_callers_own_handle_is_reported_available()
    {
        var me = UserAccount.Register("sub-1", "me@example.com", "Me", null, Now);
        me.SetUsername("aziz");
        _accounts.GetByUsernameAsync("aziz", Arg.Any<CancellationToken>()).Returns(me);

        var result = await CreateHandler().Handle(
            new CheckUsernameAvailabilityQuery("aziz", me.Id), CancellationToken.None);

        result.IsAvailable.Should().BeTrue();
    }
}
