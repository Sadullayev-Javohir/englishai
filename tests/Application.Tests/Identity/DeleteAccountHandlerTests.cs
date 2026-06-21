using Application.Common;
using Application.Gamification.Ports;
using Application.Identity.DeleteAccount;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Identity;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Identity;

public class DeleteAccountHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 27, 9, 0, 0, TimeSpan.Zero);

    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IAccountEraser _eraser = Substitute.For<IAccountEraser>();
    private readonly IGamificationStore _gamification = Substitute.For<IGamificationStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly ILeaderboardStore _leaderboard = Substitute.For<ILeaderboardStore>();

    private DeleteAccountCommandHandler CreateHandler() =>
        new(_accounts, _eraser, _gamification, _profiles, _leaderboard);

    private UserAccount GivenAccount(string email = "me@example.com")
    {
        var account = UserAccount.Register("sub-1", email, "Aziz", null, Now);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        return account;
    }

    [Fact]
    public async Task Erases_data_and_clears_gamification_when_email_matches()
    {
        var account = GivenAccount();

        await CreateHandler().Handle(
            new DeleteAccountCommand(account.Id, "me@example.com"), CancellationToken.None);

        await _eraser.Received(1).EraseAsync(account.Id, Arg.Any<CancellationToken>());
        await _gamification.Received(1).DeleteLearnerAsync(account.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_match_is_case_and_whitespace_insensitive()
    {
        var account = GivenAccount("Me@Example.com");

        await CreateHandler().Handle(
            new DeleteAccountCommand(account.Id, "  me@example.COM  "), CancellationToken.None);

        await _eraser.Received(1).EraseAsync(account.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Mismatched_email_throws_conflict_and_erases_nothing()
    {
        var account = GivenAccount();

        var act = () => CreateHandler().Handle(
            new DeleteAccountCommand(account.Id, "someone-else@example.com"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _eraser.DidNotReceive().EraseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _gamification.DidNotReceive().DeleteLearnerAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_account_throws_not_found()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var act = () => CreateHandler().Handle(
            new DeleteAccountCommand(Guid.NewGuid(), "me@example.com"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _eraser.DidNotReceive().EraseAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
