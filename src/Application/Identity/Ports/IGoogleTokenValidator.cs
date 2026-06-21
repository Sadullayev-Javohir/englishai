namespace Application.Identity.Ports;

/// <summary>
/// The verified identity carried by a Google ID token after its signature, issuer,
/// audience and lifetime have been validated.
/// </summary>
public sealed record GoogleUserInfo(
    string Subject,
    string Email,
    bool EmailVerified,
    string DisplayName,
    string? PictureUrl);

/// <summary>
/// Port that validates a Google ID token (the JWT minted by Google Identity Services on
/// the client) and returns the verified identity. The Infrastructure adapter checks the
/// signature against Google's published keys and the audience against the configured
/// OAuth client id (docs/development-guide.md rule 10 - external calls behind a port).
/// </summary>
public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates the token. Returns the verified identity, or <c>null</c> when the token
    /// is invalid, tampered with, expired, or issued for a different client.
    /// </summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
