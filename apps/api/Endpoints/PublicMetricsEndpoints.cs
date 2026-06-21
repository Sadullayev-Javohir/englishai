using Application.Analytics.Dtos;
using Application.Analytics.GetPublicSocialProofMetrics;
using Application.Identity.Ports;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Web.Endpoints;

public static class PublicMetricsEndpoints
{
    private const string CacheKey = "public-social-proof-metrics";

    public static IEndpointRouteBuilder MapPublicMetricsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/metrics", async (
            ISender sender,
            IUserAccountStore accounts,
            IMemoryCache cache,
            CancellationToken cancellationToken) =>
        {
            var metrics = await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return await sender.Send(new GetPublicSocialProofMetricsQuery(), cancellationToken);
            });

            var registeredUsers = await accounts.CountAsync(cancellationToken);
            var response = metrics ?? new PublicSocialProofMetricsDto(
                DateOnly.FromDateTime(DateTime.UtcNow), 0, 0, 0, 0, 0, 0);

            return Results.Ok(response with { RegisteredUsers = registeredUsers });
        }).AllowAnonymous().WithTags("Public");

        return app;
    }
}
