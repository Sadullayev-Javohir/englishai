using Application.Retention.EvaluateChurnRisk;
using Application.Retention.GetFeatureAssignment;
using Application.Retention.GetFeatureAssignments;
using Application.Retention.GetRetentionMetrics;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for retention/growth (PROJECT-SPEC Qism I): per-learner churn risk and
/// feature-flag assignments, plus the cohort retention metrics dashboard. Thin: forwards
/// to MediatR with no business logic. The daily win-back sweep runs via Hangfire, not here.
/// </summary>
public static class RetentionEndpoints
{
    public static IEndpointRouteBuilder MapRetentionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/retention").WithTags("Retention");

        // Per-learner drop-off risk (PROJECT-SPEC I.1).
        group.MapGet("/{learnerId:guid}/churn", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new EvaluateChurnRiskQuery(learnerId))));

        // All feature-flag experiments with the learner's assigned variant (PROJECT-SPEC I.3).
        group.MapGet("/{learnerId:guid}/flags", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetFeatureAssignmentsQuery(learnerId))));

        // A single experiment's variant; 404 when the flag key is undefined.
        group.MapGet("/{learnerId:guid}/flags/{key}", async (Guid learnerId, string key, ISender sender) =>
        {
            var assignment = await sender.Send(new GetFeatureAssignmentQuery(learnerId, key));
            return assignment is null ? Results.NotFound() : Results.Ok(assignment);
        });

        // Cohort D1/D7/D30 retention for the growth dashboard (PROJECT-SPEC I.4).
        group.MapGet("/metrics", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetRetentionMetricsQuery())));

        return app;
    }
}
