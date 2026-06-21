using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Identity.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

/// <summary>
/// Validates Google ID tokens (the JWTs minted by Google Identity Services on the client)
/// without a Google SDK dependency: it fetches Google's published signing keys from their
/// OpenID Connect metadata (cached and rotated by <see cref="ConfigurationManager{T}"/>)
/// and verifies the token's signature, issuer, audience and lifetime. Returns the verified
/// identity, or null when anything fails to check out.
/// </summary>
public sealed class GoogleIdTokenValidator : IGoogleTokenValidator
{
    // Google mints tokens with either issuer form; both are valid (Google docs).
    private static readonly string[] ValidIssuers = { "https://accounts.google.com", "accounts.google.com" };
    private const string MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";

    private readonly GoogleAuthOptions _options;
    private readonly ILogger<GoogleIdTokenValidator> _logger;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
    private readonly JwtSecurityTokenHandler _handler = new();

    public GoogleIdTokenValidator(GoogleAuthOptions options, ILogger<GoogleIdTokenValidator> logger)
    {
        _options = options;
        _logger = logger;
        _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            MetadataAddress, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever());
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogError("Google sign-in is not configured: Auth:Google:ClientId is missing.");
            return null;
        }

        try
        {
            var config = await _configManager.GetConfigurationAsync(cancellationToken);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers = ValidIssuers,
                ValidateAudience = true,
                ValidAudience = _options.ClientId,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
            };

            var principal = _handler.ValidateToken(idToken, parameters, out _);
            return Map(principal);
        }
        catch (Exception ex)
        {
            // An invalid/expired/forged token is an expected outcome, not a server fault.
            _logger.LogWarning(ex, "Google ID token validation failed.");
            return null;
        }
    }

    private static GoogleUserInfo? Map(ClaimsPrincipal principal)
    {
        var subject = Find(principal, "sub") ?? Find(principal, ClaimTypes.NameIdentifier);
        var email = Find(principal, "email") ?? Find(principal, ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            return null;

        var emailVerified = string.Equals(Find(principal, "email_verified"), "true",
            StringComparison.OrdinalIgnoreCase);
        var name = Find(principal, "name") ?? Find(principal, ClaimTypes.Name) ?? email;
        var picture = Find(principal, "picture");

        return new GoogleUserInfo(subject, email, emailVerified, name, picture);
    }

    private static string? Find(ClaimsPrincipal principal, string type) =>
        principal.FindFirst(type)?.Value;
}
