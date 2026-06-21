using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Common;

namespace Web;

/// <summary>
/// Resolves the authenticated learner from the current request's JWT <c>sub</c> claim
/// (the session cookie). Returns null when there is no authenticated user, so the
/// ownership behavior simply does not enforce anything for anonymous/background paths.
/// </summary>
public sealed class HttpContextCurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? LearnerId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
