using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Referral.GetReferralStatus;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the referral programme (the Profile "Invite friends" section). Thin:
/// forwards to MediatR. The learner is always the authenticated caller (resolved from the
/// session), never a route value, so a caller can only ever read their own referral standing.
/// </summary>
public static class ReferralEndpoints
{
    public static IEndpointRouteBuilder MapReferralEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/referral").WithTags("Referral");

        // The signed-in learner's code, share stats, and earned bonus. Creates the learner's
        // referral account (and code) on first read so the share link works immediately.
        group.MapGet("", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetReferralStatusQuery(userId.Value)));
        }).RequireAuthorization();

        return app;
    }

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
