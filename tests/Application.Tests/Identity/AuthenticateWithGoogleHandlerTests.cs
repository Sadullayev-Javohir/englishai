using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.AuthenticateWithGoogle;
using Application.Identity.DevLogin;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Referral;
using Application.Tests.Learning;
using Domain.Analytics;
using Domain.Assessment;
using Domain.Identity;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Identity;

public class AuthenticateWithGoogleHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private readonly IGoogleTokenValidator _google = Substitute.For<IGoogleTokenValidator>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IAuthTokenIssuer _issuer = Substitute.For<IAuthTokenIssuer>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IReferralService _referrals = Substitute.For<IReferralService>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private AuthenticateWithGoogleCommandHandler CreateHandler()
    {
        _issuer.Issue(Arg.Any<UserAccount>())
            .Returns(ci => new IssuedAuthToken("jwt-token", Now.AddDays(30)));
        return new AuthenticateWithGoogleCommandHandler(
            _google, _accounts, _issuer, _profiles, _referrals, _events, _clock);
    }

    private void GivenGoogleReturns(bool emailVerified = true)
    {
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleUserInfo("sub-1", "user@example.com", emailVerified, "Aziz", "https://pic"));
    }

    [Fact]
    public async Task First_login_registers_a_new_account_and_issues_a_token()
    {
        GivenGoogleReturns();
        _accounts.GetByGoogleSubjectAsync("sub-1", Arg.Any<CancellationToken>())
            .Returns((UserAccount?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new AuthenticateWithGoogleCommand("id-token"), CancellationToken.None);

        result.User.Email.Should().Be("user@example.com");
        result.User.DisplayName.Should().Be("Aziz");
        result.User.HasOnboarded.Should().BeFalse(); // brand-new account has no profile yet
        result.Token.Should().Be("jwt-token");
        await _accounts.Received(1).AddAsync(Arg.Is<UserAccount>(a => a.GoogleSubject == "sub-1"), Arg.Any<CancellationToken>());
        await _accounts.DidNotReceive().UpdateAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
        await _events.Received(1).AppendOnceAsync(
            result.User.Id,
            ProductEventType.Registered,
            Now,
            "google",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returning_login_reuses_the_existing_account()
    {
        GivenGoogleReturns();
        var existing = UserAccount.Register("sub-1", "old@example.com", "Old", null, Now.AddYears(-1));
        _accounts.GetByGoogleSubjectAsync("sub-1", Arg.Any<CancellationToken>()).Returns(existing);
        _profiles.GetByLearnerIdAsync(existing.Id, Arg.Any<CancellationToken>())
            .Returns(LearnerProfile.CreateAtLevel(existing.Id, CefrLevel.A2, Now.AddYears(-1)));
        var handler = CreateHandler();

        var result = await handler.Handle(new AuthenticateWithGoogleCommand("id-token"), CancellationToken.None);

        result.User.Id.Should().Be(existing.Id);
        result.User.Email.Should().Be("user@example.com"); // refreshed from Google
        result.User.HasOnboarded.Should().BeTrue(); // returning learner already has a profile
        await _accounts.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await _accounts.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
        await _events.Received(1).AppendAsync(
            Arg.Is<ProductEvent>(e =>
                e.LearnerId == existing.Id &&
                e.Type == ProductEventType.SignedIn &&
                e.OccurredAt == Now &&
                e.Source == "google"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_already_owned_by_a_different_google_subject_throws_conflict()
    {
        GivenGoogleReturns();
        _accounts.GetByGoogleSubjectAsync("sub-1", Arg.Any<CancellationToken>())
            .Returns((UserAccount?)null);
        var otherAccount = UserAccount.Register("sub-2", "user@example.com", "Someone Else", null, Now.AddDays(-2));
        _accounts.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>())
            .Returns(otherAccount);
        var handler = CreateHandler();

        var act = () => handler.Handle(new AuthenticateWithGoogleCommand("id-token"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _accounts.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_token_throws_unauthorized()
    {
        _google.ValidateAsync("id-token", Arg.Any<CancellationToken>()).Returns((GoogleUserInfo?)null);
        var handler = CreateHandler();

        var act = () => handler.Handle(new AuthenticateWithGoogleCommand("id-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Unverified_email_is_rejected()
    {
        GivenGoogleReturns(emailVerified: false);
        var handler = CreateHandler();

        var act = () => handler.Handle(new AuthenticateWithGoogleCommand("id-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        await _accounts.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }
}

public class DevLoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Existing_email_owner_is_reused_when_dev_subject_is_absent()
    {
        var accounts = Substitute.For<IUserAccountStore>();
        var issuer = Substitute.For<IAuthTokenIssuer>();
        var profiles = Substitute.For<ILearnerProfileRepository>();
        var referrals = Substitute.For<IReferralService>();
        var events = Substitute.For<IProductEventStore>();
        var existing = UserAccount.Register(
            "real-google-subject",
            "javohirsadullayev836@gmail.com",
            "Existing",
            null,
            Now.AddDays(-1));
        accounts.GetByEmailAsync("javohirsadullayev836@gmail.com", Arg.Any<CancellationToken>())
            .Returns(existing);
        issuer.Issue(existing).Returns(new IssuedAuthToken("jwt-token", Now.AddDays(30)));
        var handler = new DevLoginCommandHandler(
            accounts,
            issuer,
            profiles,
            referrals,
            events,
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new DevLoginCommand(), CancellationToken.None);

        result.User.Id.Should().Be(existing.Id);
        existing.GoogleSubject.Should().Be("real-google-subject");
        await accounts.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
        await accounts.DidNotReceive().AddAsync(Arg.Any<UserAccount>(), Arg.Any<CancellationToken>());
    }
}
