namespace Infrastructure.Identity;

/// <summary>
/// Configuration for the platform's own session JWT, bound from the "Auth:Jwt" section.
/// The signing key is a secret (docs/development-guide.md rule 13) supplied via user-secrets/env; when
/// left blank in development a random key is generated at startup so the app still runs
/// without secrets (tokens simply do not survive a restart).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    /// <summary>Symmetric HMAC-SHA256 signing key (base64 or any sufficiently long string).</summary>
    public string? SigningKey { get; set; }

    public string Issuer { get; set; } = "EnglishAI";
    public string Audience { get; set; } = "EnglishAI";

    /// <summary>How long an issued session token (and its cookie) stays valid.</summary>
    public int ExpiryDays { get; set; } = 30;

    /// <summary>Name of the HttpOnly cookie that carries the session token.</summary>
    public string CookieName { get; set; } = "englishai_auth";
}
