namespace Infrastructure.Identity;

/// <summary>
/// Configuration for Google sign-in, bound from the "Auth:Google" section. The client id
/// is the OAuth 2.0 Web client id created in Google Cloud Console; it is public (the SPA
/// embeds it too) but the ID token's audience is verified against it so tokens minted for
/// another app are rejected. Supplied via user-secrets/env (docs/development-guide.md rule 13).
/// </summary>
public sealed class GoogleAuthOptions
{
    public const string SectionName = "Auth:Google";

    public string? ClientId { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId);
}
