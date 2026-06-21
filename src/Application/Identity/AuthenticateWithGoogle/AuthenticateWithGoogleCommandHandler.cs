using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Referral;
using Domain.Analytics;
using Domain.Identity;
using MediatR;

namespace Application.Identity.AuthenticateWithGoogle;

public sealed class AuthenticateWithGoogleCommandHandler
    : IRequestHandler<AuthenticateWithGoogleCommand, AuthenticateWithGoogleResult>
{
    private readonly IGoogleTokenValidator _googleValidator;
    private readonly IUserAccountStore _accounts;
    private readonly IAuthTokenIssuer _tokenIssuer;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IReferralService _referrals;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;

    public AuthenticateWithGoogleCommandHandler(
        IGoogleTokenValidator googleValidator,
        IUserAccountStore accounts,
        IAuthTokenIssuer tokenIssuer,
        ILearnerProfileRepository profiles,
        IReferralService referrals,
        IProductEventStore productEvents,
        TimeProvider clock)
    {
        _googleValidator = googleValidator;
        _accounts = accounts;
        _tokenIssuer = tokenIssuer;
        _profiles = profiles;
        _referrals = referrals;
        _productEvents = productEvents;
        _clock = clock;
    }

    public async Task<AuthenticateWithGoogleResult> Handle(
        AuthenticateWithGoogleCommand request,
        CancellationToken cancellationToken)
    {
        var identity = await _googleValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (identity is null)
            throw new UnauthorizedException("Google identity could not be verified.");

        // Only let verified Google emails through - an unverified address could be a
        // throwaway or hijacked one (Google's own guidance).
        if (!identity.EmailVerified)
            throw new UnauthorizedException("Google account email is not verified.");

        var now = _clock.GetUtcNow();

        // Look the account up by the stable Google subject so a changed email never
        // splits one person into two accounts (and their learning progress).
        var account = await _accounts.GetByGoogleSubjectAsync(identity.Subject, cancellationToken);
        if (account is null)
        {
            // A different Google subject may report an email another account already owns
            // (e.g. re-registering with a second Google account tied to the same address).
            // Without this check that would silently create a second account and split the
            // learner's progress across two rows sharing one email.
            var emailOwner = await _accounts.GetByEmailAsync(
                identity.Email.Trim().ToLowerInvariant(), cancellationToken);
            if (emailOwner is not null)
                throw new ConflictException("This email is already registered to a different Google account.");

            account = UserAccount.Register(
                identity.Subject, identity.Email, identity.DisplayName, identity.PictureUrl, now);
            await _accounts.AddAsync(account, cancellationToken);

            // Only a brand-new account can be attributed to a referral. A blank/invalid/unknown
            // code is silently ignored inside the service so it never blocks sign-up.
            await _referrals.CaptureAsync(account.Id, request.ReferralCode, now, cancellationToken);
            await _productEvents.AppendOnceAsync(
                account.Id, ProductEventType.Registered, now, source: "google", cancellationToken);
        }
        else
        {
            account.RecordLogin(identity.Email, identity.DisplayName, identity.PictureUrl, now);
            await _accounts.UpdateAsync(account, cancellationToken);
            await _productEvents.AppendAsync(
                ProductEvent.Record(account.Id, ProductEventType.SignedIn, now, source: "google"), cancellationToken);
        }

        // A returning learner already has a profile (resumed progress); a freshly registered
        // one does not, so the SPA routes them into onboarding (level choice).
        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);

        var issued = _tokenIssuer.Issue(account);
        return new AuthenticateWithGoogleResult(
            AuthenticatedUserDto.From(account, profile),
            issued.Token,
            issued.ExpiresAt);
    }
}
