using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.Ports;
using Application.Identity.UpdateProfile;
using Application.Learning.Ports;
using Application.Tests.Learning;
using Domain.Assessment;
using Domain.Identity;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Identity;

public class UpdateProfileHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();

    private UpdateProfileCommandHandler CreateHandler() =>
        new(_accounts, _profiles, _events, new FixedTimeProvider(Now));

    private UserAccount GivenAccount(string? username = null)
    {
        var account = UserAccount.Register("sub-1", "me@example.com", "Aziz", null, Now);
        if (username is not null) account.SetUsername(username);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);
        return account;
    }

    [Fact]
    public async Task Sets_username_and_display_name_then_persists()
    {
        var account = GivenAccount();
        _accounts.GetByUsernameAsync("aziz_k", Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var result = await CreateHandler().Handle(
            new UpdateProfileCommand(account.Id, "Aziz Karimov", "Aziz_K"), CancellationToken.None);

        result.Username.Should().Be("aziz_k");
        result.DisplayName.Should().Be("Aziz Karimov");
        account.Username.Should().Be("aziz_k");
        await _accounts.Received(1).UpdateAsync(account, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Taken_username_throws_conflict_and_does_not_persist()
    {
        var account = GivenAccount();
        var other = UserAccount.Register("sub-2", "o@example.com", "Other", null, Now);
        other.SetUsername("aziz_k");
        _accounts.GetByUsernameAsync("aziz_k", Arg.Any<CancellationToken>()).Returns(other);

        var act = () => CreateHandler().Handle(
            new UpdateProfileCommand(account.Id, "Aziz Karimov", "aziz_k"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _accounts.DidNotReceive().UpdateAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Keeping_the_same_username_skips_the_uniqueness_lookup()
    {
        var account = GivenAccount(username: "aziz_k");

        await CreateHandler().Handle(
            new UpdateProfileCommand(account.Id, "Yangi Ism", "aziz_k"), CancellationToken.None);

        await _accounts.DidNotReceive().GetByUsernameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        account.DisplayName.Should().Be("Yangi Ism");
    }

    [Fact]
    public async Task Reports_onboarded_when_a_learner_profile_exists()
    {
        var account = GivenAccount(username: "aziz_k");
        _profiles.GetByLearnerIdAsync(account.Id, Arg.Any<CancellationToken>())
            .Returns(LearnerProfile.CreateAtLevel(account.Id, CefrLevel.A2, Now));

        var result = await CreateHandler().Handle(
            new UpdateProfileCommand(account.Id, "Aziz", "aziz_k"), CancellationToken.None);

        result.HasOnboarded.Should().BeTrue();
    }

    [Fact]
    public async Task Missing_account_throws_not_found()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var act = () => CreateHandler().Handle(
            new UpdateProfileCommand(Guid.NewGuid(), "Aziz", "aziz_k"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
