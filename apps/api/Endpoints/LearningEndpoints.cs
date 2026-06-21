using Application.Analytics.GetStudyStats;
using Application.Analytics.RecordStudyTime;
using Application.Analytics.TrackProductEvent;
using Application.Learning.GetGrowth;
using Application.Learning.GetLearnerOverview;
using Application.Learning.GetProgressDashboard;
using Application.Learning.GetProgressInsight;
using Application.Learning.GetPublicLearnerProgress;
using Application.Learning.GetRecommendations;
using Application.Learning.GetRecommendedTopics;
using Application.Learning.RecordConfirmationTest;
using Application.Learning.RecordSkillActivity;
using Application.Learning.StartLearning;
using Domain.Analytics;
using Domain.Learning;
using MediatR;

namespace Web.Endpoints;

/// <summary>Body of a study-time heartbeat (the learner id comes from the route).</summary>
public sealed record RecordStudyTimeRequest(SkillType Skill, int Seconds, DateOnly LocalDate);

/// <summary>Body for client-observed product activation events.</summary>
public sealed record TrackProductEventRequest(ProductEventType EventType, string? Source);

/// <summary>
/// HTTP surface for the learner model and analytics (PROJECT-SPEC Faza 2). Endpoints
/// are thin: they forward to MediatR and return the result. No business logic here.
/// </summary>
public static class LearningEndpoints
{
    public static IEndpointRouteBuilder MapLearningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/learning").WithTags("Learning");

        // Onboarding: create the learner's profile at a chosen starting level when they
        // skip the placement test ("Start from A1").
        group.MapPost("/start", async (StartLearningCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/activity", async (RecordSkillActivityCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/confirmation-test", async (RecordConfirmationTestCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapGet("/{learnerId:guid}/overview", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetLearnerOverviewQuery(learnerId))));

        group.MapGet("/{learnerId:guid}/recommendations", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetRecommendationsQuery(learnerId))));

        // Goal-tailored topic suggestions for the home "for your goal" strip (goal-based onboarding).
        group.MapGet("/{learnerId:guid}/recommended-topics", async (Guid learnerId, int? count, ISender sender) =>
            Results.Ok(await sender.Send(new GetRecommendedTopicsQuery(learnerId, count ?? 6))));

        group.MapGet("/{learnerId:guid}/growth", async (Guid learnerId, int? weeks, ISender sender) =>
            Results.Ok(await sender.Send(new GetGrowthQuery(learnerId, weeks ?? 8))));

        // Study-time heartbeat: the active learning page posts elapsed seconds for a skill on the
        // learner's local day. Returns 204 - the dashboard re-reads study-stats separately.
        group.MapPost("/{learnerId:guid}/study-time",
            async (Guid learnerId, RecordStudyTimeRequest body, ISender sender) =>
            {
                await sender.Send(new RecordStudyTimeCommand(learnerId, body.Skill, body.Seconds, body.LocalDate));
                return Results.NoContent();
            });

        // Client-observed activation signals (e.g. paywall opened from a locked card, pronunciation
        // feedback shown). Authoritative milestones are still written server-side in their handlers.
        group.MapPost("/{learnerId:guid}/events",
            async (Guid learnerId, TrackProductEventRequest body, ISender sender) =>
            {
                await sender.Send(new TrackProductEventCommand(learnerId, body.EventType, body.Source));
                return Results.NoContent();
            });

        // Aggregated study-time statistics for the progress dashboard. `today` is the learner's
        // local calendar day so the week/month/year boundaries match their own clock.
        group.MapGet("/{learnerId:guid}/study-stats",
            async (Guid learnerId, DateOnly? today, ISender sender) =>
                Results.Ok(await sender.Send(new GetStudyStatsQuery(
                    learnerId, today ?? DateOnly.FromDateTime(DateTime.UtcNow)))));

        group.MapGet("/{learnerId:guid}/progress-insight",
            async (Guid learnerId, string? today, ISender sender) =>
            {
                if (!TryResolveDate(today, out var asOf))
                    return Results.BadRequest(new { error = "invalid_today" });

                return Results.Ok(await sender.Send(new GetProgressInsightQuery(learnerId, asOf)));
            })
            .RequireAuthorization();

        group.MapGet("/{learnerId:guid}/progress-dashboard",
            async (Guid learnerId, string? today, ISender sender) =>
            {
                if (!TryResolveDate(today, out var asOf))
                    return Results.BadRequest(new { error = "invalid_today" });

                return Results.Ok(await sender.Send(new GetProgressDashboardQuery(learnerId, asOf)));
            })
            .RequireAuthorization();

        group.MapGet("/{learnerId:guid}/public-progress",
            async (Guid learnerId, string? today, ISender sender) =>
            {
                if (!TryResolveDate(today, out var asOf))
                    return Results.BadRequest(new { error = "invalid_today" });

                return Results.Ok(await sender.Send(new GetPublicLearnerProgressQuery(learnerId, asOf)));
            })
            .RequireAuthorization();

        return app;
    }

    private static bool TryResolveDate(string? value, out DateOnly date)
    {
        if (value is null)
        {
            date = DateOnly.FromDateTime(DateTime.UtcNow);
            return true;
        }

        return DateOnly.TryParseExact(value, "yyyy-MM-dd", out date);
    }
}
