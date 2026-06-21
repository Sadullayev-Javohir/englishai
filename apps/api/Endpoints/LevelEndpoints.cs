using Application.Levels.FinalizeLevelExitTest;
using Application.Levels.GetLevelMap;
using Application.Levels.StartLevelExitTest;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the Level Map (PROJECT-SPEC M.3) - the level-centric outer navigation layer.
/// Thin: forwards to MediatR, no business logic.
/// </summary>
public static class LevelEndpoints
{
    public static IEndpointRouteBuilder MapLevelEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/levels").WithTags("Levels");

        // The map for one CEFR level: can-do statements, ordered topics, progress and exit-test
        // readiness. When `level` is omitted the learner's current level is used.
        group.MapGet("/map/{learnerId:guid}", async (Guid learnerId, CefrLevel? level, ISender sender) =>
            Results.Ok(await sender.Send(new GetLevelMapQuery(learnerId, level))));

        // Level Exit Test (M.5). Start pins the adaptive engine to the learner's current level; the
        // questions/audio in between reuse the shared /api/placement/* endpoints (same session
        // store); finalize records the G.4 confirmation gate and advances the level when earned.
        group.MapPost("/exit-test/start", async (StartLevelExitTestCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/exit-test/finalize", async (FinalizeLevelExitTestCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        return app;
    }
}
