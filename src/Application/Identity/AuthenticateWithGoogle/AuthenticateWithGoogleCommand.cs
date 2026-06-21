using Application.Identity.Dtos;
using MediatR;

namespace Application.Identity.AuthenticateWithGoogle;

/// <summary>
/// Signs a user in (or registers them on first use) from a Google ID token produced by
/// Google Identity Services on the client. Returns the session token for the Web layer to
/// place in an HttpOnly cookie, alongside the authenticated user.
/// </summary>
/// <param name="IdToken">The Google ID token to verify.</param>
/// <param name="ReferralCode">
/// Optional referral code the visitor arrived with (from a <c>?ref=</c> share link). Only used
/// when this call registers a brand-new account; ignored for returning logins and silently
/// dropped if blank/invalid/unknown so a bad code can never block sign-in.
/// </param>
public sealed record AuthenticateWithGoogleCommand(string IdToken, string? ReferralCode = null)
    : IRequest<AuthenticateWithGoogleResult>;

/// <summary>The minted session token (+ expiry) and the user it belongs to.</summary>
public sealed record AuthenticateWithGoogleResult(
    AuthenticatedUserDto User,
    string Token,
    DateTimeOffset ExpiresAt);
