using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.AuthenticateWithGoogle;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Application.Referral;
using Domain.Analytics;
using Domain.Identity;
using MediatR;

namespace Application.Identity.DevLogin;

/// <summary>
/// Mints a dev session for a throwaway local account so the app can be driven end-to-end
/// without Google OAuth. Reuses the same account store + token issuer as the real Google
/// path, so every downstream feature behaves identically. The account is keyed by a stable
/// dev subject so repeated dev logins resume the same profile instead of multiplying rows.
/// </summary>
public sealed class DevLoginCommandHandler
    : IRequestHandler<DevLoginCommand, AuthenticateWithGoogleResult>
{
    private const string DevSubject = "dev-local-subject";
    // The local dev account logs in as the operator super-admin so the whole admin surface
    // (incl. /admin/users) is reachable in DEV PREVIEW without a real Google OAuth round-trip.
    private const string DevEmail = "javohirsadullayev836@gmail.com";
    private const string DevName = "Javohir";

    private readonly IUserAccountStore _accounts;
    private readonly IAuthTokenIssuer _tokenIssuer;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IReferralService _referrals;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;

    public DevLoginCommandHandler(
        IUserAccountStore accounts,
        IAuthTokenIssuer tokenIssuer,
        ILearnerProfileRepository profiles,
        IReferralService referrals,
        IProductEventStore productEvents,
        TimeProvider clock)
    {
        _accounts = accounts;
        _tokenIssuer = tokenIssuer;
        _profiles = profiles;
        _referrals = referrals;
        _productEvents = productEvents;
        _clock = clock;
    }

    public async Task<AuthenticateWithGoogleResult> Handle(
        DevLoginCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();

        var account = await _accounts.GetByGoogleSubjectAsync(DevSubject, cancellationToken)
            ?? await _accounts.GetByEmailAsync(DevEmail, cancellationToken);
        if (account is null)
        {
            account = UserAccount.Register(DevSubject, DevEmail, DevName, null, now);
            await _accounts.AddAsync(account, cancellationToken);
            await _productEvents.AppendOnceAsync(
                account.Id, ProductEventType.Registered, now, source: "dev", cancellationToken);
        }
        else
        {
            account.RecordLogin(DevEmail, DevName, null, now);
            await _accounts.UpdateAsync(account, cancellationToken);
            await _productEvents.AppendAsync(
                ProductEvent.Record(account.Id, ProductEventType.SignedIn, now, source: "dev"),
                cancellationToken);
        }

        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);

        var issued = _tokenIssuer.Issue(account);
        return new AuthenticateWithGoogleResult(
            AuthenticatedUserDto.From(account, profile),
            issued.Token,
            issued.ExpiresAt);
    }
}
