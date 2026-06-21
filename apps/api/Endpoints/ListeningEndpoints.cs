using Application.Listening.CheckListeningAnswer;
using Application.Listening.GetListeningAudio;
using Application.Listening.GetListeningCatalog;
using Application.Listening.GetListeningExercise;
using Application.Listening.SubmitListeningQuiz;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the Listening module (PROJECT-SPEC Faza 4 - leveled audio + comprehension
/// quiz). Endpoints are thin: they forward to MediatR and return the result. The audio clip is
/// synthesized once (Azure TTS) and cached behind the audio endpoint (rule 10).
/// </summary>
public static class ListeningEndpoints
{
    public static IEndpointRouteBuilder MapListeningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/listening").WithTags("Listening");

        // `level` browses one CEFR band; `all=true` returns the whole catalog easiest-first.
        // With neither, the list is adapted to the learner's own level.
        group.MapGet("/catalog/{learnerId:guid}", async (
            Guid learnerId, CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetListeningCatalogQuery(learnerId, level, all ?? false))));

        // The catalog key is the learning-spine topic id; the exercise is generated + cached on open.
        group.MapGet("/topic/{topicId:guid}", async (Guid topicId, ISender sender) =>
            Results.Ok(await sender.Send(new GetListeningExerciseQuery(topicId))));

        // The synthesized clip - the learner must hear it. Synthesized once and cached (rule 10).
        // enableRangeProcessing makes the response honour HTTP Range requests (206 Partial Content):
        // without it the byte[] is always served whole with 200, so the browser cannot seek and any
        // scrub on an un-buffered clip snaps playback back to the start. With it, seeking works.
        //
        // AllowAnonymous: the clip is played by a plain <audio src> element, which (unlike the
        // fetch client) cannot attach the native Bearer token, so behind the global auth policy it
        // would 401 inside the Capacitor shell and the audio would silently fail to load. The
        // content is non-sensitive TTS keyed only by topic id (no learner data), so it is served
        // anonymously - the same reason the image endpoints opt out (consumed by <img src>).
        group.MapGet("/topic/{topicId:guid}/audio", async (Guid topicId, ISender sender) =>
        {
            var result = await sender.Send(new GetListeningAudioQuery(topicId));
            return result.PublicUrl is not null
                ? Results.Redirect(result.PublicUrl)
                : Results.File(result.Audio!, result.ContentType, lastModified: result.CreatedAt,
                    entityTag: result.ETag is null ? null : new Microsoft.Net.Http.Headers.EntityTagHeaderValue(result.ETag),
                    enableRangeProcessing: true);
        }).AllowAnonymous();

        group.MapPost("/answers/check", async (CheckListeningAnswerQuery query, ISender sender) =>
            Results.Ok(await sender.Send(query)));

        group.MapPost("/quiz", async (SubmitListeningQuizCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        return app;
    }
}
