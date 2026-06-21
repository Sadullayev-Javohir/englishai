using Domain.Identity;

namespace Application.Identity.Ports;

/// <summary>A signed session token plus the moment it expires.</summary>
public sealed record IssuedAuthToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>
/// Port that mints the platform's own session JWT for an authenticated account. The Web
/// layer places this token in an HttpOnly cookie (docs/development-guide.md §3). Decoupled from any
/// concrete JWT library so the signing scheme can change behind the port.
/// </summary>
public interface IAuthTokenIssuer
{
    IssuedAuthToken Issue(UserAccount account);
}
