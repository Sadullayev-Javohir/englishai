using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Identity.Ports;
using Domain.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Identity;

/// <summary>
/// Mints the platform's own session JWT (HMAC-SHA256) for an authenticated account. The
/// token carries the account id as <c>sub</c>, which the SPA uses as the learner id. The
/// signing key and lifetime come from <see cref="JwtOptions"/>.
/// </summary>
public sealed class JwtAuthTokenIssuer : IAuthTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _clock;

    public JwtAuthTokenIssuer(JwtOptions options, TimeProvider clock)
    {
        _options = options;
        _clock = clock;
    }

    public IssuedAuthToken Issue(UserAccount account)
    {
        var now = _clock.GetUtcNow();
        var expiresAt = now.AddDays(_options.ExpiryDays);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new("name", account.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedAuthToken(encoded, expiresAt);
    }
}
