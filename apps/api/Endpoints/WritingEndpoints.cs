using Application.Writing.GetWritingCatalog;
using Application.Writing.GetWritingTask;
using Application.Writing.SubmitWriting;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the Writing module (PROJECT-SPEC G.3 - topic-scoped prompts + 4-dimension AI
/// assessment). Every skill teaches the same learning-spine topics, so the catalog is the 50 topics
/// for the learner's level and each task is keyed by its topic id (generated + cached on first open).
/// Endpoints are thin: they forward to MediatR and return the result. The submit endpoint is gated
/// (Free: 3 assessments/month) and returns HTTP 402 when the limit is exceeded (handled by the global
/// exception middleware).
/// </summary>
public static class WritingEndpoints
{
    public static IEndpointRouteBuilder MapWritingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/writing").WithTags("Writing");

        // `level` browses one CEFR band; `all=true` returns the whole catalog easiest-first.
        group.MapGet("/catalog/{learnerId:guid}", async (
            Guid learnerId, CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetWritingCatalogQuery(learnerId, level, all ?? false))));

        // The catalog key is the learning-spine topic id; the task is generated + cached on open.
        group.MapGet("/topic/{topicId:guid}", async (Guid topicId, ISender sender) =>
            Results.Ok(await sender.Send(new GetWritingTaskQuery(topicId))));

        group.MapPost("/submit", async (SubmitWritingCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        return app;
    }
}
