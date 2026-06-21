using Application.Assessment.FinalizePlacementTest;
using Application.Assessment.GetPlacementAudio;
using Application.Assessment.ResumePlacementTest;
using Application.Assessment.ReportIntegrityViolation;
using Application.Assessment.StartPlacementTest;
using Application.Assessment.SubmitAnswer;
using Application.Assessment.SubmitSpeaking;
using Application.Assessment.SubmitWriting;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the CEFR placement test. Endpoints are thin: they forward the
/// request to MediatR and return the result. No business logic lives here.
/// </summary>
public static class PlacementEndpoints
{
    public static IEndpointRouteBuilder MapPlacementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/placement").WithTags("Placement");

        group.MapPost("/start", async (StartPlacementTestCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapGet("/session/{sessionId:guid}", async (Guid sessionId, ISender sender) =>
            Results.Ok(await sender.Send(new ResumePlacementTestQuery(sessionId))));

        group.MapPost("/answer", async (SubmitAnswerCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        // Productive stages: free-text Writing and recorded Speaking answers. Audio is sent
        // as a JSON byte array (same encoding as the Speaking module's utterance endpoint).
        group.MapPost("/answer/writing", async (SubmitWritingCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/answer/speaking", async (SubmitSpeakingCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/finalize", async (FinalizePlacementTestCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        // Logged, not just recorded: an invalidated placement leaves the learner without a CEFR
        // level, so which signal fired and on which question has to be visible in the logs.
        group.MapPost("/integrity-violation", async (
            ReportPlacementIntegrityViolationCommand command,
            ISender sender,
            ILoggerFactory loggerFactory) =>
        {
            var result = await sender.Send(command);
            loggerFactory.CreateLogger("Placement.Integrity").LogInformation(
                "Placement integrity violation: session {SessionId}, reason {Reason}, stage {Stage}, "
                + "item {ItemNumber}/{TotalItems}, count {Count}/{Threshold}, invalidated {Invalidated}.",
                command.SessionId,
                command.Reason,
                result.Stage,
                result.ItemNumber,
                result.TotalItems,
                result.ViolationCount,
                PlacementTestSession.ViolationsBeforeInvalidation,
                result.Invalidated);
            return Results.Ok(result);
        });

        // Streams the spoken clip for a listening item. The script text is never sent
        // to the client (the learner must hear it); audio is synthesized once and
        // cached behind the query handler.
        //
        // AllowAnonymous: played by a plain <audio src> element, which cannot attach the native
        // Bearer token, so behind the global auth policy it would 401 inside the Capacitor shell.
        // The clip is non-sensitive (TTS keyed by question id, no learner data, script withheld),
        // so it is served anonymously - same rationale as the listening/image media endpoints.
        group.MapGet("/audio/{questionId:guid}", async (Guid questionId, ISender sender) =>
        {
            var result = await sender.Send(new GetPlacementAudioQuery(questionId));
            return result.PublicUrl is not null
                ? Results.Redirect(result.PublicUrl)
                : Results.File(result.Audio!, result.ContentType, lastModified: result.CreatedAt,
                    entityTag: result.ETag is null ? null : new Microsoft.Net.Http.Headers.EntityTagHeaderValue(result.ETag),
                    enableRangeProcessing: true);
        }).AllowAnonymous();

        return app;
    }
}
