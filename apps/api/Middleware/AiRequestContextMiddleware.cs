using Application.Ai;
using Application.Common;
using Infrastructure.Ai;
using Microsoft.AspNetCore.Routing;

namespace Web.Middleware;

public sealed class AiRequestContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserAccessor currentUser,
        IAiRequestContextResolver contexts)
    {
        var feature = ResolveFeature(context.Request.Path);
        var callerKey = currentUser.LearnerId?.ToString() ?? RateLimitPolicy.ResolveClientKey(context);
        var resolved = await contexts.ResolveAsync(
            feature,
            callerKey,
            context.RequestAborted);
        using var scope = AiAdmissionContext.Push(new(
            resolved.CallerKey,
            resolved.Tier,
            resolved.Feature,
            ResolveRequestPath(context)));
        await next(context);
    }

    private static string? ResolveRequestPath(HttpContext context) =>
        context.GetEndpoint() is RouteEndpoint route && !string.IsNullOrWhiteSpace(route.RoutePattern.RawText)
            ? $"/{route.RoutePattern.RawText.TrimStart('/')}"
            : context.Request.Path.Value;

    private static AiFeature ResolveFeature(PathString path)
    {
        if (path.StartsWithSegments("/api/assistant")) return AiFeature.Assistant;
        if (path.StartsWithSegments("/api/translate")) return AiFeature.Translation;
        if (path.StartsWithSegments("/api/writing")) return AiFeature.WritingAssessment;
        if (path.StartsWithSegments("/api/video")) return AiFeature.VideoExplain;
        if (path.StartsWithSegments("/api/speaking") || path.StartsWithSegments("/hubs/speaking-live"))
            return AiFeature.SpeakingTutor;
        if (path.StartsWithSegments("/api/placement")) return AiFeature.SpeakingEvaluation;
        return AiFeature.Other;
    }
}
