using Application.Grammar.CheckGrammarExercise;
using Application.Grammar.GetGrammarCatalog;
using Application.Grammar.GetGrammarLesson;
using Application.Grammar.SubmitGrammarExercises;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the Grammar module (PROJECT-SPEC G.2 - topic-scoped 5-step lessons). Each
/// topic's grammar focus is taught in the topic's own context. Endpoints are thin: they forward to
/// MediatR and return the result.
/// </summary>
public static class GrammarEndpoints
{
    public static IEndpointRouteBuilder MapGrammarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/grammar").WithTags("Grammar");

        // An optional ?level= lets the learner browse the grammar topics of any CEFR level; `all=true`
        // returns the whole A1→C2 catalog easiest-first; omitted, the catalog is for the learner's own level.
        group.MapGet("/catalog/{learnerId:guid}", async (
            Guid learnerId, CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetGrammarCatalogQuery(learnerId, level, all ?? false))));

        // The catalog key is the learning-spine topic id; the lesson is generated + cached on open.
        group.MapGet("/topic/{topicId:guid}", async (Guid topicId, ISender sender) =>
            Results.Ok(await sender.Send(new GetGrammarLessonQuery(topicId))));

        group.MapPost("/exercises/check", async (CheckGrammarExerciseQuery query, ISender sender) =>
            Results.Ok(await sender.Send(query)));

        group.MapPost("/exercises", async (SubmitGrammarExercisesCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        return app;
    }
}
