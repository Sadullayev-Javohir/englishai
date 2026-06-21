using Application.Common;
using Application.Speaking;
using Application.Speaking.AssessSegmentPronunciation;
using Application.Speaking.AccentTutors;
using Application.Speaking.PracticeWords;
using Application.Speaking.EvaluateRoleplay;
using Application.Speaking.GetFreeTalkTopics;
using Application.Speaking.GetIdeaCards;
using Application.Speaking.GetRoleplayScenarios;
using Application.Speaking.GetSpeakingQuota;
using Application.Speaking.GetWordPronunciationDetail;
using Application.Speaking.StartConversation;
using Application.Speaking.StartRoleplay;
using Application.Speaking.SubmitUtterance;
using Domain.Assessment;
using Infrastructure.Speaking;
using MediatR;
using System.Text.Json;

namespace Web.Endpoints;

/// <summary>HTTP surface for the Speaking module. Thin: forwards to MediatR.</summary>
public static class SpeakingEndpoints
{
    public static IEndpointRouteBuilder MapSpeakingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/speaking").WithTags("Speaking");

        group.MapPost("/start", async (StartConversationCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/accent-tutors/{tutorId}/start", async (
            string tutorId,
            ISender sender) =>
            Results.Ok(await sender.Send(new AccentTutorStartQuery(tutorId))))
            .RequireAuthorization();

        group.MapPost("/accent-tutors/{tutorId}/nudge", async (
            string tutorId,
            AccentTutorNudgeRequest request,
            ISender sender) =>
            Results.Ok(await sender.Send(new AccentTutorNudgeQuery(tutorId, request.History))))
            .RequireAuthorization();

        // Realtime Azure Voice Live: mint a short-lived token + connection info so the browser can
        // open a direct realtime session to the tutor's Foundry agent (no server in the audio path).
        group.MapGet("/accent-tutors/{tutorId}/voice-live/token", async (
            string tutorId,
            ISender sender) =>
            Results.Ok(await sender.Send(new GetAccentTutorVoiceLiveConnectionQuery(tutorId))))
            .RequireAuthorization();

        // The browser reports back when the realtime session ends so the server can bill the real
        // duration instead of leaving the up-front reservation standing. Deliberately not rate
        // limited: refusing a completion report strands the (larger) reservation and costs money.
        group.MapPost("/accent-tutors/{tutorId}/voice-live/complete", async (
            string tutorId,
            CompleteAccentTutorVoiceLiveSessionRequest request,
            ISender sender) =>
            Results.Ok(await sender.Send(new CompleteAccentTutorVoiceLiveSessionCommand(
                tutorId,
                request.SessionId,
                request.DurationSeconds))))
            .RequireAuthorization();

        group.MapPost("/accent-tutors/{tutorId}/turn", async (
            string tutorId,
            AccentTutorTurnRequest request,
            ISender sender) =>
        {
            if (request.AudioContent is not { Length: > 0 and <= 2_000_000 })
                return Results.BadRequest(new { message = "Audio payload size is invalid." });
            return Results.Ok(await sender.Send(new AccentTutorTurnCommand(
                tutorId,
                request.AudioContent,
                request.History,
                request.IsInterruption)));
        }).RequireAuthorization();

        group.MapPost("/accent-tutors/{tutorId}/evaluate", async (
            string tutorId,
            AccentTutorEvaluateRequest request,
            ISender sender) =>
            Results.Ok(await sender.Send(new AccentTutorEvaluateCommand(tutorId, request.History, request.Scores))))
            .RequireAuthorization();

        group.MapPost("/accent-tutors/{tutorId}/turn/stream", async (
            string tutorId,
            AccentTutorTurnRequest request,
            IServiceProvider services,
            HttpContext http) =>
        {
            if (request.AudioContent is not { Length: > 0 and <= 2_000_000 })
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                await http.Response.WriteAsJsonAsync(new { message = "Audio payload size is invalid." });
                return;
            }

            var handler = services.GetRequiredService<IRequestHandler<AccentTutorTurnCommand, AccentTutorTurnResult>>()
                as AccentTutorTurnCommandHandler
                ?? throw new InvalidOperationException("Accent tutor handler is unavailable.");
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache, no-transform";
            http.Response.Headers.Connection = "keep-alive";
            http.Response.Headers.Append("X-Accel-Buffering", "no");

            async Task Emit(string eventName, object payload)
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                await http.Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
            }

            try
            {
                await Emit("status", new { phase = "recognizing" });
                await handler.ProcessAsync(
                    new AccentTutorTurnCommand(tutorId, request.AudioContent, request.History, request.IsInterruption),
                    Emit,
                    http.RequestAborted);
                await http.Response.WriteAsync("event: done\ndata: {}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
            }
            catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
            {
                // The learner left or explicitly cancelled the live turn.
            }
            catch (Exception exception)
            {
                var unavailable = exception as SpeakingTutorUnavailableException;
                await Emit("error", new
                {
                    code = unavailable?.Code ?? (exception.Message.StartsWith("Speech could not be recognized", StringComparison.Ordinal)
                        ? "speech_not_recognized"
                        : "accent_tutor_provider_unavailable"),
                    message = unavailable?.Message ?? "Speaking AI is temporarily unavailable.",
                    retryable = unavailable?.Retryable ?? true,
                });
            }
        }).RequireAuthorization();

        // Read before entering the conversation room so the learner sees what is left (and the
        // paywall opens) before they speak rather than after.
        group.MapGet("/quota/{learnerId:guid}", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetSpeakingQuotaQuery(learnerId))));

        group.MapPost("/utterance", async (SubmitUtteranceCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/utterance/stream", async (
            SubmitUtteranceCommand command,
            IServiceProvider services,
            HttpContext http) =>
        {
            var handler = services.GetRequiredService<IRequestHandler<SubmitUtteranceCommand, SubmitUtteranceResult>>()
                as SubmitUtteranceCommandHandler
                ?? throw new InvalidOperationException("Speaking utterance handler is unavailable.");
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers.Connection = "keep-alive";

            async Task Emit(string eventName, object payload)
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                await http.Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
            }

            try
            {
                await handler.ProcessAsync(command, Emit, http.RequestAborted);
                await http.Response.WriteAsync("event: done\ndata: {}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
            }
            catch (OperationCanceledException) when (http.RequestAborted.IsCancellationRequested)
            {
                // The browser left the page or canceled the turn.
            }
            catch (SpeakingTutorUnavailableException exception)
            {
                await Emit("tutor-unavailable", new
                {
                    code = exception.Code,
                    message = exception.Message,
                    retryable = exception.Retryable,
                });
            }
            catch (SpeakingMinutesExhaustedException exception)
            {
                // Response headers are already sent, so this cannot become a 402: an exception here
                // would truncate the stream and the client would see a network error instead of the
                // upgrade prompt. Emit it as a typed event the client maps to the same paywall.
                await Emit("quota-exhausted", new
                {
                    code = SpeakingMinutesExhaustedException.Code,
                    message = exception.Message,
                    limitMinutes = exception.LimitMinutes,
                    usedMinutes = exception.UsedMinutes,
                    resetsAt = exception.ResetsAt,
                });
            }
            catch (Exception exception)
            {
                var json = JsonSerializer.Serialize(new { message = exception.Message });
                await http.Response.WriteAsync($"event: error\ndata: {json}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
            }
        });

        group.MapGet("/word/{word}", async (string word, ISender sender) =>
            Results.Ok(await sender.Send(new GetWordPronunciationDetailQuery(word))));

        group.MapGet("/practice-words/{learnerId:guid}", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetSpeakingPracticeWordsQuery(learnerId))));

        group.MapPost("/practice-words/{practiceWordId:guid}/attempt", async (
            Guid practiceWordId,
            SubmitSpeakingPracticeAttemptRequest request,
            ISender sender) =>
            Results.Ok(await sender.Send(new SubmitSpeakingPracticeAttemptCommand(
                practiceWordId, request.AudioContent))));

        // Shadowing: scores one video transcript line the learner spoke along with, against that
        // line's own known text (a slice of one continuous recording - see the video player's
        // shadowing mode). Reference text is already known, so unlike /utterance no STT pass is
        // needed first.
        group.MapPost("/segment-pronunciation", async (AssessSegmentPronunciationCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        // "I don't know what to say" helper: concrete talking-point idea cards for a live
        // conversation, keyed by the session so they fit its topic/level/recent turns. Resilient by
        // design - always returns usable cards (never errors) so a stuck learner is never blocked.
        group.MapPost("/idea-cards", async (GetIdeaCardsQuery query, ISender sender) =>
            Results.Ok(await sender.Send(query)));

        // Free-talk conversation topics ("Erkin suhbat"): the curated catalog of subjects the learner
        // can chat about, 20 per CEFR level. `level` narrows to one level's 20 topics; omitting it (or
        // passing all=true) returns the whole A1→C2 catalog. The client sends a topic's code back to
        // /start as the conversation topic.
        group.MapGet("/free-talk-topics", async (
            CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetFreeTalkTopicsQuery(level, all ?? false))));

        // Roleplay: list the scenarios (20 per CEFR level, 120 total), start one (persona greeting),
        // and score the finished sitting. `level` narrows to one level's 20 scenarios; omitting it
        // (or passing all=true) returns the whole A1→C2 catalog. Turns are submitted through the
        // shared /utterance endpoint above; the client only ever sends a scenario's code.
        group.MapGet("/roleplay/scenarios", async (
            CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetRoleplayScenariosQuery(level, all ?? false))));

        group.MapPost("/roleplay/start", async (StartRoleplayCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/roleplay/evaluate", async (EvaluateRoleplayCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        return app;
    }

    public sealed record SubmitSpeakingPracticeAttemptRequest(byte[] AudioContent);
}

public sealed record AccentTutorTurnRequest(
    byte[] AudioContent,
    IReadOnlyList<AccentTutorMessage>? History,
    bool IsInterruption = false);
public sealed record AccentTutorNudgeRequest(
    IReadOnlyList<AccentTutorMessage>? History);
public sealed record AccentTutorEvaluateRequest(
    IReadOnlyList<AccentTutorMessage>? History,
    IReadOnlyList<AccentTutorAttemptScore>? Scores);
public sealed record CompleteAccentTutorVoiceLiveSessionRequest(
    string SessionId,
    double DurationSeconds);
